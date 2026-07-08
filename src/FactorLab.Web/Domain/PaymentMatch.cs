namespace FactorLab.Web.Domain;

public sealed class PaymentMatch
{
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    public string Reference { get; init; } = "";
    public string InvoiceNumber { get; init; } = "";
    public string Debtor { get; init; } = "";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public PaymentMatchStatus Status { get; init; }
    public string Note { get; init; } = "";
}
