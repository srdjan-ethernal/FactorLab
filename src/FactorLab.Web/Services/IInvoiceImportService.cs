using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IInvoiceImportService
{
    InvoiceImportResult Import(string csv, IPortfolioRepository portfolio);
}
