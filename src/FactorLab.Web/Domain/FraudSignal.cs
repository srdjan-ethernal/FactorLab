namespace FactorLab.Web.Domain;

public sealed class FraudSignal
{
    public string SignalType { get; init; } = "";
    public string Entity { get; init; } = "";
    public FraudSignalSeverity Severity { get; init; }
    public string Detail { get; init; } = "";
    public string RecommendedAction { get; init; } = "";
}
