namespace FactorLab.Web.Services;

public sealed class BorrowingBaseSummary
{
    public decimal GrossReceivables { get; init; }
    public decimal EligibleReceivables { get; init; }
    public decimal BorrowingBase { get; init; }
    public decimal MaxAdvance { get; init; }
    public decimal ExistingAdvance { get; init; }
    public decimal Availability { get; init; }
}
