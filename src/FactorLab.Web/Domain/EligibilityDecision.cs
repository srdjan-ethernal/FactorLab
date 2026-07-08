namespace FactorLab.Web.Domain;

public sealed class EligibilityDecision
{
    public EligibilityDecision(bool isEligible, IReadOnlyList<string> blockers, IReadOnlyList<string> warnings, int score)
    {
        IsEligible = isEligible;
        Blockers = blockers;
        Warnings = warnings;
        Score = score;
    }

    public bool IsEligible { get; }
    public IReadOnlyList<string> Blockers { get; }
    public IReadOnlyList<string> Warnings { get; }
    public int Score { get; }

    public IReadOnlyList<string> Reasons =>
        Blockers.Count > 0 || Warnings.Count > 0
            ? Blockers.Concat(Warnings).ToArray()
            : new[] { "Ready to fund" };
}
