using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface ITemplateGenerationService
{
    GeneratedTemplate Generate(TemplateKind kind, Invoice invoice, IPortfolioRepository portfolio, FactoringTerms terms);
}
