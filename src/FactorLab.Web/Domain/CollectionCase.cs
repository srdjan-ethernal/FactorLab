namespace FactorLab.Web.Domain;

public sealed class CollectionCase
{
    public string InvoiceNumber { get; set; } = "";
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public CollectionStatus Status { get; set; } = CollectionStatus.NotDue;
    public DateTime? PromiseToPayDate { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChargebackAmount { get; set; }
    public decimal ReserveReleased { get; set; }
    public string Owner { get; set; } = "";
    public string NextAction { get; set; } = "";

    public int DaysPastDue => Math.Max(0, (DateTime.UtcNow.Date - DueDate.Date).Days);
}
