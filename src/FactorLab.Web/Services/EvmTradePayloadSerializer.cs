using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public static class EvmTradePayloadSerializer
{
    public static string Serialize(EvmTradeEvent tradeEvent) =>
        string.Join("|",
            tradeEvent.Action,
            tradeEvent.InvoiceNumber,
            tradeEvent.ClientName,
            tradeEvent.Debtor,
            tradeEvent.Counterparty,
            tradeEvent.Reference,
            tradeEvent.Amount.ToString("0.00"),
            tradeEvent.Currency);
}
