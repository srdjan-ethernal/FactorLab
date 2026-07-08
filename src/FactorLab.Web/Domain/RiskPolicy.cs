namespace FactorLab.Web.Domain;

public sealed class RiskPolicy
{
    public int MinimumScore { get; set; } = 48;
    public int MaximumTenorDays { get; set; } = 120;
    public int LongTenorReviewDays { get; set; } = 90;
    public decimal ConcentrationWarningPercent { get; set; } = 35m;
    public decimal FacilityWarningUtilizationPercent { get; set; } = 85m;
    public decimal CreditWarningUtilizationPercent { get; set; } = 85m;
    public decimal HighDilutionPercent { get; set; } = 8m;
    public int SlowPaymentDays { get; set; } = 75;
}
