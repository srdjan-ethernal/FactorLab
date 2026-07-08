namespace FactorLab.Web.Domain;

public sealed class ClientOffer
{
    public string OfferNumber { get; set; } = "";
    public string ClientName { get; set; } = "";
    public ClientOfferStatus Status { get; set; } = ClientOfferStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.Date.AddDays(7);
    public string SentToEmail { get; set; } = "";
    public string AcceptedBy { get; set; } = "";
    public DateTime? AcceptedAt { get; set; }
    public int InvoiceCount { get; set; }
    public decimal GrossReceivables { get; set; }
    public decimal AdvanceAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal ReserveHeld { get; set; }
    public decimal NetCash { get; set; }
    public decimal WeightedDays { get; set; }
    public decimal EffectiveApr { get; set; }
    public string InvoiceNumbers { get; set; } = "";
    public string PortalToken { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    public string Notes { get; set; } = "";

    public bool IsActionable => Status is ClientOfferStatus.Draft or ClientOfferStatus.Sent;
}
