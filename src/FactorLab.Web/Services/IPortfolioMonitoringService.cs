using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IPortfolioMonitoringService
{
    IReadOnlyList<CovenantCheck> BuildChecks(IPortfolioRepository portfolio, FactoringTerms terms);
}
