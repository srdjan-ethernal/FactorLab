using System.Security.Cryptography;
using System.Text;
using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class LocalEvmLedgerService : IEvmLedgerService
{
    private readonly IEvmTradeSubmitter submitter;

    public LocalEvmLedgerService(IEvmTradeSubmitter submitter)
    {
        this.submitter = submitter;
    }

    public EvmTradeEvent RecordTrade(IPortfolioRepository portfolio, Invoice invoice, EvmTradeAction action, string reference, string actor, string counterparty)
    {
        var existing = portfolio.EvmTradeEvents.FirstOrDefault(item =>
            item.Action == action &&
            item.InvoiceNumber.Equals(invoice.InvoiceNumber, StringComparison.OrdinalIgnoreCase) &&
            item.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase));

        if (existing is not null) return existing;

        var tradeEvent = new EvmTradeEvent
        {
            Action = action,
            InvoiceNumber = invoice.InvoiceNumber,
            ClientName = invoice.ClientName,
            Debtor = invoice.Debtor,
            Counterparty = counterparty,
            Reference = reference,
            Amount = invoice.Amount,
            Currency = portfolio.Terms.Currency,
            Actor = actor,
            Note = $"{action} queued for EVM settlement."
        };

        tradeEvent.PayloadHash = Hash(EvmTradePayloadSerializer.Serialize(tradeEvent));
        var submission = submitter.Submit(tradeEvent);
        tradeEvent.ChainId = submission.ChainId;
        tradeEvent.NetworkName = submission.NetworkName;
        tradeEvent.ContractAddress = submission.ContractAddress;
        tradeEvent.PayloadHash = submission.PayloadHash;
        tradeEvent.TransactionHash = submission.TransactionHash;
        tradeEvent.Status = EvmTransactionStatus.Submitted;
        tradeEvent.SubmittedAt = DateTime.UtcNow;
        tradeEvent.Note = submission.Note;

        portfolio.EvmTradeEvents.Add(tradeEvent);
        return tradeEvent;
    }

    public IReadOnlyList<EvmTradeEvent> RecordOfferAcceptance(IPortfolioRepository portfolio, ClientOffer offer, string actor)
    {
        var events = new List<EvmTradeEvent>();
        foreach (var invoiceNumber in ParseInvoiceNumbers(offer.InvoiceNumbers))
        {
            var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
            if (invoice is not null)
            {
                events.Add(RecordTrade(portfolio, invoice, EvmTradeAction.SellReceivable, offer.OfferNumber, actor, "FactorLab SPV"));
            }
        }

        return events;
    }

    public IReadOnlyList<EvmTradeEvent> RecordFundingBatch(IPortfolioRepository portfolio, FundingBatch batch, string actor)
    {
        var events = new List<EvmTradeEvent>();
        foreach (var invoiceNumber in ParseInvoiceNumbers(batch.InvoiceNumbers))
        {
            var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
            if (invoice is not null)
            {
                events.Add(RecordTrade(portfolio, invoice, EvmTradeAction.BuyReceivable, batch.BatchNumber, actor, invoice.ClientName));
            }
        }

        return events;
    }

    public void MarkSubmitted(EvmTradeEvent tradeEvent)
    {
        if (tradeEvent.Status == EvmTransactionStatus.Confirmed) return;

        tradeEvent.Status = EvmTransactionStatus.Submitted;
        tradeEvent.SubmittedAt ??= DateTime.UtcNow;
        tradeEvent.Note = "Transaction submitted to EVM RPC provider.";
    }

    public void MarkConfirmed(EvmTradeEvent tradeEvent)
    {
        tradeEvent.Status = EvmTransactionStatus.Confirmed;
        tradeEvent.SubmittedAt ??= DateTime.UtcNow;
        tradeEvent.ConfirmedAt = DateTime.UtcNow;
        tradeEvent.Note = "Transaction confirmed on EVM chain.";
    }

    private static IEnumerable<string> ParseInvoiceNumbers(string invoiceNumbers) =>
        invoiceNumbers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"0x{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
