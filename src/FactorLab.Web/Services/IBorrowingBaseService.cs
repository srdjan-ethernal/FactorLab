using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IBorrowingBaseService
{
    IReadOnlyList<BorrowingBaseLine> Build(IPortfolioRepository portfolio, FactoringTerms terms);
    BorrowingBaseSummary Summarize(IReadOnlyList<BorrowingBaseLine> lines);
}
