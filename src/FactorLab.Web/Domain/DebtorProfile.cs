namespace FactorLab.Web.Domain;

public sealed class DebtorProfile
{
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public string Country { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public DebtorRating Rating { get; set; } = DebtorRating.B;
    public BuyDecision BuyDecision { get; set; } = BuyDecision.Buy;
    public int AverageDaysToPay { get; set; }
    public decimal DilutionPercent { get; set; }
}
