namespace FactorLab.Web.Domain;

public sealed class FundingBatch
{
    public string BatchNumber { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string CreatedBy { get; init; } = "";
    public int InvoiceCount { get; init; }
    public decimal GrossReceivables { get; init; }
    public decimal AdvanceAmount { get; init; }
    public decimal EstimatedFees { get; init; }
    public decimal ReserveHeld { get; init; }
    public decimal NetCash { get; init; }
    public string Currency { get; init; } = "EUR";
    public string InvoiceNumbers { get; init; } = "";
}
