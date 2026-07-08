namespace FactorLab.Web.Domain;

public sealed class CollectionActivity
{
    public string InvoiceNumber { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public CollectionActivityType ActivityType { get; set; }
    public string ContactName { get; set; } = "";
    public string Note { get; set; } = "";
}
