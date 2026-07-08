namespace FactorLab.Web.Services;

public sealed class InvoiceImportResult
{
    public int Added { get; init; }
    public int Updated { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;
}
