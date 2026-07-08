using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IReportExportService
{
    ReportExport Export(ReportKind kind, IPortfolioRepository portfolio, FactoringTerms terms);
}
