namespace FactorLab.Web.Services;

public sealed class DocumentExtractionResult
{
    public bool IsAvailable { get; init; }
    public string Summary { get; init; } = "";
    public Dictionary<string, string> Fields { get; init; } = new();
}
