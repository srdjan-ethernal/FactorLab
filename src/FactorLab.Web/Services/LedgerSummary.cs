namespace FactorLab.Web.Services;

public sealed class LedgerSummary
{
    public decimal Advances { get; init; }
    public decimal Fees { get; init; }
    public decimal ReserveHeld { get; init; }
    public decimal PaymentsReceived { get; init; }
    public decimal ReserveReleased { get; init; }
    public decimal Chargebacks { get; init; }
    public decimal NetOutstanding => Math.Max(0m, Advances + Fees + ReserveHeld - PaymentsReceived - ReserveReleased + Chargebacks);
}
