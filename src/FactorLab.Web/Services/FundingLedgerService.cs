using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class FundingLedgerService : IFundingLedgerService
{
    public IReadOnlyList<LedgerEntry> BuildLedger(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        var entries = new List<LedgerEntry>();
        var fundedStages = new[] { FundingStage.Approved, FundingStage.Funded, FundingStage.Collected, FundingStage.Settled };

        foreach (var invoice in portfolio.Invoices.Where(invoice => fundedStages.Contains(invoice.FundingStage)))
        {
            var advance = invoice.Amount * terms.AdvanceRate / 100m;
            var reserve = invoice.Amount - advance;
            var fee = Math.Max(
                terms.MinimumFee,
                invoice.Amount * terms.DiscountRatePer30Days / 100m * invoice.DaysToPay / 30m + invoice.Amount * terms.ServiceFeeRate / 100m);

            entries.Add(Entry(invoice, LedgerEntryType.Advance, advance, 0m, terms.Currency, "Cash advanced to client."));
            entries.Add(Entry(invoice, LedgerEntryType.Fee, fee, 0m, terms.Currency, "Estimated discount and service fee."));
            entries.Add(Entry(invoice, LedgerEntryType.ReserveHeld, reserve, 0m, terms.Currency, "Reserve retained until debtor payment."));

            var collectionCase = portfolio.CollectionCases.FirstOrDefault(item => item.InvoiceNumber == invoice.InvoiceNumber);
            if (collectionCase is null) continue;

            if (collectionCase.AmountPaid > 0)
            {
                entries.Add(Entry(invoice, LedgerEntryType.PaymentReceived, 0m, collectionCase.AmountPaid, terms.Currency, "Payment received from debtor."));
            }

            if (collectionCase.ReserveReleased > 0)
            {
                entries.Add(Entry(invoice, LedgerEntryType.ReserveReleased, 0m, collectionCase.ReserveReleased, terms.Currency, "Reserve released to client."));
            }

            if (collectionCase.ChargebackAmount > 0)
            {
                entries.Add(Entry(invoice, LedgerEntryType.Chargeback, collectionCase.ChargebackAmount, 0m, terms.Currency, "Chargeback recoverable from client."));
            }
        }

        return entries
            .OrderByDescending(item => item.PostedAt)
            .ThenBy(item => item.InvoiceNumber)
            .ToArray();
    }

    public LedgerSummary Summarize(IReadOnlyList<LedgerEntry> entries) => new()
    {
        Advances = entries.Where(item => item.EntryType == LedgerEntryType.Advance).Sum(item => item.Debit),
        Fees = entries.Where(item => item.EntryType == LedgerEntryType.Fee).Sum(item => item.Debit),
        ReserveHeld = entries.Where(item => item.EntryType == LedgerEntryType.ReserveHeld).Sum(item => item.Debit),
        PaymentsReceived = entries.Where(item => item.EntryType == LedgerEntryType.PaymentReceived).Sum(item => item.Credit),
        ReserveReleased = entries.Where(item => item.EntryType == LedgerEntryType.ReserveReleased).Sum(item => item.Credit),
        Chargebacks = entries.Where(item => item.EntryType == LedgerEntryType.Chargeback).Sum(item => item.Debit)
    };

    private static LedgerEntry Entry(Invoice invoice, LedgerEntryType entryType, decimal debit, decimal credit, string currency, string note) => new()
    {
        InvoiceNumber = invoice.InvoiceNumber,
        ClientName = invoice.ClientName,
        Debtor = invoice.Debtor,
        EntryType = entryType,
        Debit = debit,
        Credit = credit,
        Currency = currency,
        Note = note
    };
}
