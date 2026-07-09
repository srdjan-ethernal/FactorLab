using FactorLab.Web.Domain;
using Microsoft.Extensions.Options;

namespace FactorLab.Web.Services;

public sealed class EvmRpcTradeSubmitter : IEvmTradeSubmitter
{
    private readonly EvmContractOptions options;

    public EvmRpcTradeSubmitter(IOptions<EvmContractOptions> options)
    {
        this.options = options.Value;
    }

    public EvmSubmissionResult Submit(EvmTradeEvent tradeEvent)
    {
        if (string.IsNullOrWhiteSpace(options.RpcUrl))
        {
            throw new InvalidOperationException("EVM RPC URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.OperatorAddress))
        {
            throw new InvalidOperationException("EVM operator address is not configured.");
        }

        throw new NotImplementedException("Wire this adapter to a Web3 signer. The contract ABI is in contracts/FactorLabReceivables.sol.");
    }
}
