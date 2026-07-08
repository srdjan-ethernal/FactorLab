namespace FactorLab.Web.Domain;

public sealed class FactoringSummary
{
    public decimal EligibleAmount { get; init; }
    public decimal Advance { get; init; }
    public decimal Reserve { get; init; }
    public decimal TotalCost { get; init; }
    public decimal CashToday { get; init; }
    public decimal WeightedDays { get; init; }
    public decimal EffectiveApr { get; init; }
    public decimal BankCost { get; init; }
    public int PortfolioQuality { get; init; }
    public int EligibleCount { get; init; }
    public int DocumentReadyCount { get; init; }
    public int MissingRequiredDocuments { get; init; }
}
