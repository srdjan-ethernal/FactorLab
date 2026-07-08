using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class FundingBatchService : IFundingBatchService
{
    public FundingBatch? CreateApprovedBatch(IPortfolioRepository portfolio, FactoringTerms terms, string createdBy)
    {
        var invoices = portfolio.Invoices
            .Where(invoice => invoice.FundingStage == FundingStage.Approved)
            .ToArray();
        if (invoices.Length == 0) return null;

        var gross = invoices.Sum(invoice => invoice.Amount);
        var advance = gross * terms.AdvanceRate / 100m;
        var reserve = gross - advance;
        var fees = invoices.Sum(invoice =>
            Math.Max(
                terms.MinimumFee,
                invoice.Amount * terms.DiscountRatePer30Days / 100m * invoice.DaysToPay / 30m + invoice.Amount * terms.ServiceFeeRate / 100m));
        var batch = new FundingBatch
        {
            BatchNumber = $"BATCH-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            CreatedBy = createdBy,
            InvoiceCount = invoices.Length,
            GrossReceivables = gross,
            AdvanceAmount = advance,
            EstimatedFees = fees,
            ReserveHeld = reserve,
            NetCash = Math.Max(0m, advance - fees),
            Currency = terms.Currency,
            InvoiceNumbers = string.Join(", ", invoices.Select(invoice => invoice.InvoiceNumber))
        };

        foreach (var invoice in invoices)
        {
            invoice.FundingStage = FundingStage.Funded;
        }

        portfolio.FundingBatches.Add(batch);
        return batch;
    }
}
