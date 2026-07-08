using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IFraudSignalService
{
    IReadOnlyList<FraudSignal> Detect(IPortfolioRepository portfolio);
}
