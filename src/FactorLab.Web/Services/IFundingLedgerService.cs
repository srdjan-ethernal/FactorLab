using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IFundingLedgerService
{
    IReadOnlyList<LedgerEntry> BuildLedger(IPortfolioRepository portfolio, FactoringTerms terms);
    LedgerSummary Summarize(IReadOnlyList<LedgerEntry> entries);
}
