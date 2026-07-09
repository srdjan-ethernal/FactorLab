using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public interface IEvmTradeSubmitter
{
    EvmSubmissionResult Submit(EvmTradeEvent tradeEvent);
}
