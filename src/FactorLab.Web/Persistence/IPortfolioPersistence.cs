namespace FactorLab.Web.Persistence;

public interface IPortfolioPersistence
{
    string ProviderName { get; }
    bool IsDurable { get; }
    int SaveChanges();
    void Reload();
}
