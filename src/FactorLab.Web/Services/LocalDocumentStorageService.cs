using FactorLab.Web.Domain;
using Microsoft.AspNetCore.Components.Forms;

namespace FactorLab.Web.Services;

public sealed class LocalDocumentStorageService : IDocumentStorageService
{
    private const long MaxFileSize = 15 * 1024 * 1024;
    private readonly IWebHostEnvironment environment;

    public LocalDocumentStorageService(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

    public async Task<StoredDocumentInfo> SaveAsync(string invoiceNumber, DocumentKind kind, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        var safeInvoice = SafeSegment(invoiceNumber);
        var safeKind = SafeSegment(kind.ToString());
        var safeFileName = SafeSegment(Path.GetFileName(file.Name));
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "Uploads", safeInvoice, safeKind);
        Directory.CreateDirectory(folder);

        var storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{safeFileName}";
        var fullPath = Path.Combine(folder, storedFileName);

        await using var input = file.OpenReadStream(MaxFileSize, cancellationToken);
        await using var output = File.Create(fullPath);
        await input.CopyToAsync(output, cancellationToken);

        return new StoredDocumentInfo
        {
            FileName = file.Name,
            StoredPath = Path.GetRelativePath(environment.ContentRootPath, fullPath),
            ContentType = file.ContentType,
            SizeBytes = file.Size
        };
    }

    private static string SafeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "document" : cleaned;
    }
}
