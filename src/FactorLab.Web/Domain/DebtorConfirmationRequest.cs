namespace FactorLab.Web.Domain;

public sealed class DebtorConfirmationRequest
{
    public string RequestNumber { get; init; } = "";
    public string InvoiceNumber { get; init; } = "";
    public string Debtor { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime DueAt { get; set; } = DateTime.UtcNow.Date.AddDays(3);
    public DebtorConfirmationStatus Status { get; set; } = DebtorConfirmationStatus.Draft;
    public string SentBy { get; set; } = "";
    public string ResponseNote { get; set; } = "";
    public DateTime? RespondedAt { get; set; }
}
