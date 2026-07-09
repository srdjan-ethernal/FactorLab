namespace FactorLab.Web.Services;

public sealed record EvmSubmissionResult(
    int ChainId,
    string NetworkName,
    string ContractAddress,
    string PayloadHash,
    string TransactionHash,
    string Note);
