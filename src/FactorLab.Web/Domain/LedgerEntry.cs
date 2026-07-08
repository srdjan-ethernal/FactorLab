namespace FactorLab.Web.Domain;

public sealed class LedgerEntry
{
    public DateTime PostedAt { get; init; } = DateTime.UtcNow;
    public string InvoiceNumber { get; init; } = "";
    public string ClientName { get; init; } = "";
    public string Debtor { get; init; } = "";
    public LedgerEntryType EntryType { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Note { get; init; } = "";
}
