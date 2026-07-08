using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IFundingBatchService
{
    FundingBatch? CreateApprovedBatch(IPortfolioRepository portfolio, FactoringTerms terms, string createdBy);
}
