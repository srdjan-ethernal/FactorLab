namespace FactorLab.Web.Domain;

public sealed class DisputeCase
{
    public string CaseNumber { get; init; } = "";
    public string InvoiceNumber { get; init; } = "";
    public DateTime OpenedAt { get; init; } = DateTime.UtcNow;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public decimal DisputedAmount { get; set; }
    public decimal CreditNoteAmount { get; set; }
    public string Reason { get; set; } = "";
    public string Owner { get; set; } = "";
    public string NextAction { get; set; } = "";
    public DateTime? ResolvedAt { get; set; }
}
