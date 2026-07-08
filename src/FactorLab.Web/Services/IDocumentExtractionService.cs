using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public interface IDocumentExtractionService
{
    DocumentExtractionResult Extract(DocumentRequirement document);
}
