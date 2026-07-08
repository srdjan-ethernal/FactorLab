namespace FactorLab.Web.Domain;

public sealed class EvmTradeEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public EvmTradeAction Action { get; set; }
    public EvmTransactionStatus Status { get; set; } = EvmTransactionStatus.Queued;
    public int ChainId { get; set; } = 11155111;
    public string NetworkName { get; set; } = "Sepolia";
    public string ContractAddress { get; set; } = "0xFAc70A0000000000000000000000000000000001";
    public string InvoiceNumber { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Debtor { get; set; } = "";
    public string Counterparty { get; set; } = "";
    public string Reference { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Actor { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public string TransactionHash { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string Note { get; set; } = "";
}
