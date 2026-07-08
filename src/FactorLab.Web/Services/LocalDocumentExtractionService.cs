using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public sealed class LocalDocumentExtractionService : IDocumentExtractionService
{
    public DocumentExtractionResult Extract(DocumentRequirement document)
    {
        if (string.IsNullOrWhiteSpace(document.FileName))
        {
            return new DocumentExtractionResult
            {
                IsAvailable = false,
                Summary = "No document uploaded yet."
            };
        }

        return new DocumentExtractionResult
        {
            IsAvailable = true,
            Summary = "Local metadata extraction only. Azure Document Intelligence can replace this service for field-level OCR.",
            Fields = new Dictionary<string, string>
            {
                ["File"] = document.FileName,
                ["Type"] = document.Kind.ToString(),
                ["Content type"] = string.IsNullOrWhiteSpace(document.ContentType) ? "sample/unknown" : document.ContentType,
                ["Size"] = document.SizeBytes > 0 ? $"{document.SizeBytes / 1024m:0.0} KB" : "sample data"
            }
        };
    }
}
