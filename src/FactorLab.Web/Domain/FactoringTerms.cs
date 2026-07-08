namespace FactorLab.Web.Domain;

public sealed class FactoringTerms
{
    public string Currency { get; set; } = "EUR";
    public decimal AdvanceRate { get; set; } = 85m;
    public decimal DiscountRatePer30Days { get; set; } = 1.45m;
    public decimal ServiceFeeRate { get; set; } = 0.35m;
    public decimal MinimumFee { get; set; } = 75m;
    public decimal BankApr { get; set; } = 12m;
    public bool IsNonRecourse { get; set; }
    public RiskPolicy RiskPolicy { get; set; } = new();
}
