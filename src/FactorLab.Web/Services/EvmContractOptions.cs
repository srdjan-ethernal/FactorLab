namespace FactorLab.Web.Services;

public sealed class EvmContractOptions
{
    public string Mode { get; set; } = "Simulated";
    public int ChainId { get; set; } = 11155111;
    public string NetworkName { get; set; } = "Sepolia";
    public string RpcUrl { get; set; } = "";
    public string ContractAddress { get; set; } = "0xFAc70A0000000000000000000000000000000001";
    public string OperatorAddress { get; set; } = "";
    public string PrivateKeySecretName { get; set; } = "FactorLab--Evm--OperatorPrivateKey";
    public int ConfirmationBlocks { get; set; } = 3;
}
