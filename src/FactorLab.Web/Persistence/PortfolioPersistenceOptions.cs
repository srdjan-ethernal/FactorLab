namespace FactorLab.Web.Persistence;

public sealed class PortfolioPersistenceOptions
{
    public string Mode { get; set; } = "Sample";
    public bool UseSampleSeedWhenEmpty { get; set; } = true;
}
