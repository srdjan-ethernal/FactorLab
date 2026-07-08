using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IActionCenterService
{
    IReadOnlyList<ActionItem> BuildQueue(IPortfolioRepository portfolio, FactoringTerms terms, IReadOnlyCollection<string> completedActionIds);
}
