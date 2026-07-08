using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class BorrowingBaseService : IBorrowingBaseService
{
    private readonly FactoringCalculator calculator;
    private readonly IFundingLedgerService ledger;

    public BorrowingBaseService(FactoringCalculator calculator, IFundingLedgerService ledger)
    {
        this.calculator = calculator;
        this.ledger = ledger;
    }

    public IReadOnlyList<BorrowingBaseLine> Build(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        var ledgerEntries = ledger.BuildLedger(portfolio, terms);

        return portfolio.Clients.Select(client =>
        {
            var invoices = portfolio.Invoices
                .Where(invoice => invoice.ClientName == client.Name && invoice.FundingStage != FundingStage.Settled)
                .ToArray();
            var gross = invoices.Sum(invoice => invoice.Amount);
            var eligible = invoices
                .Where(invoice => calculator.Decide(
                    invoice,
                    terms,
                    client,
                    portfolio.Debtors.FirstOrDefault(debtor => debtor.Name == invoice.Debtor),
                    portfolio.Invoices).IsEligible)
                .Sum(invoice => invoice.Amount);
            var ineligible = Math.Max(0m, gross - eligible);
            var concentrationCap = gross * client.ConcentrationLimitPercent / 100m;
            var concentrationExcess = invoices
                .GroupBy(invoice => invoice.Debtor)
                .Sum(group => Math.Max(0m, group.Sum(invoice => invoice.Amount) - concentrationCap));
            var debtorLimitExcess = invoices
                .GroupBy(invoice => invoice.Debtor)
                .Sum(group =>
                {
                    var debtor = portfolio.Debtors.FirstOrDefault(item => item.Name == group.Key);
                    if (debtor is null || debtor.CreditLimit <= 0) return 0m;
                    return Math.Max(0m, group.Sum(invoice => invoice.Amount) - debtor.CreditLimit);
                });
            var baseAmount = Math.Max(0m, eligible - concentrationExcess - debtorLimitExcess);
            var maxAdvance = Math.Min(client.FacilityLimit, baseAmount * terms.AdvanceRate / 100m);
            var existingAdvance = ledgerEntries
                .Where(entry => entry.ClientName == client.Name && entry.EntryType == LedgerEntryType.Advance)
                .Sum(entry => entry.Debit);
            var availability = Math.Max(0m, maxAdvance - existingAdvance);

            return new BorrowingBaseLine
            {
                ClientName = client.Name,
                FacilityLimit = client.FacilityLimit,
                GrossReceivables = gross,
                EligibleReceivables = eligible,
                IneligibleReceivables = ineligible,
                ConcentrationExcess = concentrationExcess,
                DebtorLimitExcess = debtorLimitExcess,
                BorrowingBase = baseAmount,
                MaxAdvance = maxAdvance,
                ExistingAdvance = existingAdvance,
                Availability = availability,
                Status = availability > 0m ? "Available" : maxAdvance > 0m ? "Fully utilized" : "Blocked"
            };
        }).ToArray();
    }

    public BorrowingBaseSummary Summarize(IReadOnlyList<BorrowingBaseLine> lines) => new()
    {
        GrossReceivables = lines.Sum(line => line.GrossReceivables),
        EligibleReceivables = lines.Sum(line => line.EligibleReceivables),
        BorrowingBase = lines.Sum(line => line.BorrowingBase),
        MaxAdvance = lines.Sum(line => line.MaxAdvance),
        ExistingAdvance = lines.Sum(line => line.ExistingAdvance),
        Availability = lines.Sum(line => line.Availability)
    };
}
