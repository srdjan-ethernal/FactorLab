namespace FactorLab.Web.Services;

public sealed class StoredDocumentInfo
{
    public string FileName { get; init; } = "";
    public string StoredPath { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long SizeBytes { get; init; }
}
