using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class CsvInvoiceImportService : IInvoiceImportService
{
    public InvoiceImportResult Import(string csv, IPortfolioRepository portfolio)
    {
        var added = 0;
        var updated = 0;
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
            return new InvoiceImportResult { Errors = new[] { "CSV file is empty." } };
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
                var invoiceNumber = Value(row, map, "invoice", "invoiceNumber", "invoice_no");
                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    errors.Add($"Row {rowNumber}: missing invoice number.");
                    continue;
                }

                var existing = portfolio.Invoices.FirstOrDefault(invoice =>
                    invoice.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
                var invoice = existing ?? new Invoice { InvoiceNumber = invoiceNumber };
                invoice.ClientName = Value(row, map, "client", "clientName") ?? portfolio.Clients.FirstOrDefault()?.Name ?? "";
                invoice.Debtor = Required(row, map, rowNumber, "debtor");
                invoice.Amount = DecimalValue(row, map, rowNumber, "amount");
                invoice.DaysToPay = IntValue(row, map, rowNumber, "days", "daysToPay", "tenor");
                invoice.Rating = EnumValue(row, map, rowNumber, DebtorRating.B, "rating");
                invoice.ConcentrationPercent = DecimalValue(row, map, rowNumber, "concentration", "concentrationPercent");
                invoice.Status = EnumValue(row, map, rowNumber, InvoiceStatus.Clean, "status");
                invoice.FundingStage = EnumValue(row, map, rowNumber, FundingStage.Draft, "fundingStage", "stage");

                if (existing is null)
                {
                    invoice.Documents.AddRange(DocumentChecklistFactory.CreateDefault());
                    foreach (var document in invoice.Documents)
                    {
                        document.InvoiceNumber = invoice.InvoiceNumber;
                    }

                    portfolio.Invoices.Add(invoice);
                    portfolio.DecisionFor(invoice.InvoiceNumber);
                    portfolio.CollectionCaseFor(invoice.InvoiceNumber);
                    added++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        return new InvoiceImportResult
        {
            Added = added,
            Updated = updated,
            Errors = errors
        };
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

    private static string Required(string[] row, IReadOnlyDictionary<string, int> map, int rowNumber, params string[] names)
    {
        var value = Value(row, map, names);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"missing {names[0]}.");
        }

        return value;
    }

    private static decimal DecimalValue(string[] row, IReadOnlyDictionary<string, int> map, int rowNumber, params string[] names)
    {
        var value = Required(row, map, rowNumber, names);
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            throw new InvalidOperationException($"invalid {names[0]} value.");
        }

        return number;
    }

    private static int IntValue(string[] row, IReadOnlyDictionary<string, int> map, int rowNumber, params string[] names)
    {
        var value = Required(row, map, rowNumber, names);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            throw new InvalidOperationException($"invalid {names[0]} value.");
        }

        return number;
    }

    private static TEnum EnumValue<TEnum>(string[] row, IReadOnlyDictionary<string, int> map, int rowNumber, TEnum fallback, params string[] names)
        where TEnum : struct
    {
        var value = Value(row, map, names);
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)) return parsed;
        throw new InvalidOperationException($"invalid {names[0]} value.");
    }
}
