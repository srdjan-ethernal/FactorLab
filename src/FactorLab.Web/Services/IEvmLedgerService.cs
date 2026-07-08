using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IEvmLedgerService
{
    EvmTradeEvent RecordTrade(IPortfolioRepository portfolio, Invoice invoice, EvmTradeAction action, string reference, string actor, string counterparty);
    IReadOnlyList<EvmTradeEvent> RecordOfferAcceptance(IPortfolioRepository portfolio, ClientOffer offer, string actor);
    IReadOnlyList<EvmTradeEvent> RecordFundingBatch(IPortfolioRepository portfolio, FundingBatch batch, string actor);
    void MarkSubmitted(EvmTradeEvent tradeEvent);
    void MarkConfirmed(EvmTradeEvent tradeEvent);
}
