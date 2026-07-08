namespace FactorLab.Web.Domain;

public sealed class Invoice
{
    public string ClientName { get; set; } = "";
    public string Debtor { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public int DaysToPay { get; set; }
    public DebtorRating Rating { get; set; }
    public decimal ConcentrationPercent { get; set; }
    public InvoiceStatus Status { get; set; }
    public FundingStage FundingStage { get; set; }
    public List<DocumentRequirement> Documents { get; } = new();
}
