using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public interface IIntegrationOutboxService
{
    IReadOnlyList<IntegrationEvent> Events { get; }
    void Publish(string eventType, string entity, string summary, string target);
    void MarkDelivered(string id);
    void Retry(string id);
}
