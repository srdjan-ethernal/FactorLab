namespace FactorLab.Web.Services;

public sealed class PaymentReconciliationResult
{
    public int Matched { get; init; }
    public int Partial { get; init; }
    public int Unmatched { get; init; }
    public decimal AmountMatched { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;
}
