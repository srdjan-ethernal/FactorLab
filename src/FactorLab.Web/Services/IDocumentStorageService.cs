using FactorLab.Web.Domain;
using Microsoft.AspNetCore.Components.Forms;

namespace FactorLab.Web.Services;

public interface IDocumentStorageService
{
    Task<StoredDocumentInfo> SaveAsync(string invoiceNumber, DocumentKind kind, IBrowserFile file, CancellationToken cancellationToken = default);
}
