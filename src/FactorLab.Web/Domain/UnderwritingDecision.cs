namespace FactorLab.Web.Domain;

public sealed class UnderwritingDecision
{
    public string InvoiceNumber { get; set; } = "";
    public UnderwritingDecisionStatus Status { get; set; } = UnderwritingDecisionStatus.Pending;
    public string AssignedTo { get; set; } = "";
    public string DecisionNote { get; set; } = "";
    public string Conditions { get; set; } = "";
    public bool IsManualOverride { get; set; }
    public DateTime? DecidedAt { get; set; }
}
