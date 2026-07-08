using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class CsvPaymentReconciliationService : IPaymentReconciliationService
{
    public PaymentReconciliationResult Reconcile(string csv, IPortfolioRepository portfolio)
    {
        var matched = 0;
        var partial = 0;
        var unmatched = 0;
        var amountMatched = 0m;
        var errors = new List<string>();

        using var reader = new StringReader(csv);
        using var parser = new TextFieldParser(reader)
        {
            HasFieldsEnclosedInQuotes = true,
            TextFieldType = FieldType.Delimited,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            return new PaymentReconciliationResult { Errors = new[] { "CSV file is empty." } };
        }

        var headers = parser.ReadFields() ?? Array.Empty<string>();
        var map = headers
            .Select((name, index) => new { Name = Normalize(name), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);
        var rowNumber = 1;

        while (!parser.EndOfData)
        {
            rowNumber++;
            var row = parser.ReadFields() ?? Array.Empty<string>();
            try
            {
                var amount = DecimalValue(row, map, rowNumber, "amount", "paid", "paymentAmount");
                var reference = Value(row, map, "reference", "bankReference", "remittance") ?? $"ROW-{rowNumber}";
                var debtor = Value(row, map, "debtor") ?? "";
                var invoiceNumber = Value(row, map, "invoice", "invoiceNumber", "invoice_no") ?? "";
                var currency = Value(row, map, "currency") ?? portfolio.Terms.Currency;
                var receivedAt = DateValue(row, map, "receivedAt", "paymentDate", "date") ?? DateTime.UtcNow;
                var invoice = FindInvoice(portfolio, invoiceNumber, debtor, amount);
                if (invoice is null)
                {
                    unmatched++;
                    portfolio.PaymentMatches.Add(new PaymentMatch
                    {
                        Reference = reference,
                        InvoiceNumber = invoiceNumber,
                        Debtor = debtor,
                        Amount = amount,
                        Currency = currency,
                        ReceivedAt = receivedAt,
                        Status = PaymentMatchStatus.Unmatched,
                        Note = "No invoice match found."
                    });
                    continue;
                }

                var collectionCase = portfolio.CollectionCaseFor(invoice.InvoiceNumber);
                collectionCase.AmountPaid += amount;
                var isFullPayment = collectionCase.AmountPaid >= invoice.Amount;
                if (isFullPayment)
                {
                    collectionCase.Status = CollectionStatus.Paid;
                    collectionCase.ReserveReleased = Math.Max(0m, invoice.Amount * (100m - portfolio.Terms.AdvanceRate) / 100m);
                    collectionCase.NextAction = "Reserve released and case ready to archive";
                    invoice.FundingStage = FundingStage.Collected;
                    matched++;
                }
                else
                {
                    collectionCase.Status = CollectionStatus.PromiseToPay;
                    collectionCase.NextAction = "Partial payment received; follow up for balance";
                    partial++;
                }

                amountMatched += amount;
                portfolio.PaymentMatches.Add(new PaymentMatch
                {
                    Reference = reference,
                    InvoiceNumber = invoice.InvoiceNumber,
                    Debtor = invoice.Debtor,
                    Amount = amount,
                    Currency = currency,
                    ReceivedAt = receivedAt,
                    Status = isFullPayment ? PaymentMatchStatus.Matched : PaymentMatchStatus.Partial,
                    Note = isFullPayment ? "Payment matched and reserve released." : "Partial payment matched."
                });
                portfolio.CollectionActivities.Add(new CollectionActivity
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    ActivityType = CollectionActivityType.PaymentReceived,
                    ContactName = invoice.Debtor,
                    Note = $"{currency} {amount:0.00} payment matched from {reference}.",
                    OccurredAt = receivedAt
                });

                if (isFullPayment && collectionCase.ReserveReleased > 0)
                {
                    portfolio.CollectionActivities.Add(new CollectionActivity
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        ActivityType = CollectionActivityType.ReserveReleased,
                        ContactName = "Operations",
                        Note = $"Reserve released: {currency} {collectionCase.ReserveReleased:0.00}.",
                        OccurredAt = receivedAt
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        return new PaymentReconciliationResult
        {
            Matched = matched,
            Partial = partial,
            Unmatched = unmatched,
            AmountMatched = amountMatched,
            Errors = errors
        };
    }

    private static Invoice? FindInvoice(IPortfolioRepository portfolio, string invoiceNumber, string debtor, decimal amount)
    {
        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return portfolio.Invoices.FirstOrDefault(invoice =>
                invoice.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
        }

        return portfolio.Invoices.FirstOrDefault(invoice =>
            invoice.Debtor.Equals(debtor, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(invoice.Amount - amount) <= 1m);
    }

    private static string Normalize(string value) =>
        value.Trim().Replace(" ", "").Replace("_", "").Replace("-", "");

    private static string? Value(string[] row, IReadOnlyDictionary<string, int> map, params string[] names)
    {
        foreach (var name in names.Select(Normalize))
        {
            if (map.TryGetValue(name, out var index) && index < row.Length)
            {
                return row[index];
            }
        }

        return null;
    }

    private static decimal DecimalValue(string[] row, IReadOnlyDictionary<string, int> map, int rowNumber, params string[] names)
    {
        var value = Value(row, map, names);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"missing {names[0]}.");
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            throw new InvalidOperationException($"invalid {names[0]} value.");
        }

        return number;
    }

    private static DateTime? DateValue(string[] row, IReadOnlyDictionary<string, int> map, params string[] names)
    {
        var value = Value(row, map, names);
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }
}
