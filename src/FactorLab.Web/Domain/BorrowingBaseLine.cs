namespace FactorLab.Web.Domain;

public sealed class BorrowingBaseLine
{
    public string ClientName { get; init; } = "";
    public decimal FacilityLimit { get; init; }
    public decimal GrossReceivables { get; init; }
    public decimal EligibleReceivables { get; init; }
    public decimal IneligibleReceivables { get; init; }
    public decimal ConcentrationExcess { get; init; }
    public decimal DebtorLimitExcess { get; init; }
    public decimal BorrowingBase { get; init; }
    public decimal MaxAdvance { get; init; }
    public decimal ExistingAdvance { get; init; }
    public decimal Availability { get; init; }
    public string Status { get; init; } = "";
}
