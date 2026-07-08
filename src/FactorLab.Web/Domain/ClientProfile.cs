namespace FactorLab.Web.Domain;

public sealed class ClientProfile
{
    public string Name { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Country { get; set; } = "";
    public decimal FacilityLimit { get; set; }
    public decimal ConcentrationLimitPercent { get; set; } = 35m;
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public BuyDecision BuyDecision { get; set; } = BuyDecision.Buy;
    public string AccountManager { get; set; } = "";
}
