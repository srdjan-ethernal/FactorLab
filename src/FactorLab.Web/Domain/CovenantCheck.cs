namespace FactorLab.Web.Domain;

public sealed class CovenantCheck
{
    public string Name { get; init; } = "";
    public string Scope { get; init; } = "";
    public CovenantStatus Status { get; init; }
    public string Metric { get; init; } = "";
    public string Threshold { get; init; } = "";
    public string Action { get; init; } = "";
}
