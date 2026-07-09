namespace FactorLab.Web.Persistence;

public sealed class InMemoryPortfolioPersistence : IPortfolioPersistence
{
    public string ProviderName => "Sample in-memory";
    public bool IsDurable => false;

    public int SaveChanges() => 0;

    public void Reload()
    {
    }
}
