using System.Security.Cryptography;
using System.Text;
using FactorLab.Web.Domain;
using Microsoft.Extensions.Options;

namespace FactorLab.Web.Services;

public sealed class SimulatedEvmTradeSubmitter : IEvmTradeSubmitter
{
    private readonly EvmContractOptions options;

    public SimulatedEvmTradeSubmitter(IOptions<EvmContractOptions> options)
    {
        this.options = options.Value;
    }

    public EvmSubmissionResult Submit(EvmTradeEvent tradeEvent)
    {
        var payloadHash = string.IsNullOrWhiteSpace(tradeEvent.PayloadHash)
            ? Hash(EvmTradePayloadSerializer.Serialize(tradeEvent))
            : tradeEvent.PayloadHash;
        var txHash = Hash($"{tradeEvent.EventId}|{payloadHash}|{DateTime.UtcNow:O}|{options.ContractAddress}");

        return new EvmSubmissionResult(
            options.ChainId,
            options.NetworkName,
            options.ContractAddress,
            payloadHash,
            txHash,
            "Simulated EVM submission. Replace IEvmTradeSubmitter with an RPC signer for production.");
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"0x{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
