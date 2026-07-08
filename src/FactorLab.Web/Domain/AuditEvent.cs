namespace FactorLab.Web.Domain;

public sealed class AuditEvent
{
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Actor { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
}
