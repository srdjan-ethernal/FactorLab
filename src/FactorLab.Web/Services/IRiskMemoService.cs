using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IRiskMemoService
{
    RiskMemo Generate(Invoice invoice, IPortfolioRepository portfolio, FactoringTerms terms);
}
