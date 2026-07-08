using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public sealed class LocalIntegrationOutboxService : IIntegrationOutboxService
{
    private readonly List<IntegrationEvent> events = new()
    {
        new IntegrationEvent
        {
            EventType = "PowerBI.DatasetReady",
            Entity = "Portfolio",
            Summary = "Portfolio, exposure, underwriting and collections datasets are export-ready.",
            Target = "Power BI"
        },
        new IntegrationEvent
        {
            EventType = "Teams.DailyQueueDigest",
            Entity = "Action center",
            Summary = "Daily operational queue can be posted to an underwriting Teams channel.",
            Target = "Microsoft Teams"
        }
    };

    public IReadOnlyList<IntegrationEvent> Events => events
        .OrderByDescending(item => item.OccurredAt)
        .ToArray();

    public void Publish(string eventType, string entity, string summary, string target)
    {
        events.Add(new IntegrationEvent
        {
            EventType = eventType,
            Entity = entity,
            Summary = summary,
            Target = target
        });
    }

    public void MarkDelivered(string id)
    {
        var item = events.FirstOrDefault(candidate => candidate.Id == id);
        if (item is null) return;

        item.Status = IntegrationEventStatus.Delivered;
    }

    public void Retry(string id)
    {
        var item = events.FirstOrDefault(candidate => candidate.Id == id);
        if (item is null) return;

        item.Attempts++;
        item.Status = IntegrationEventStatus.Pending;
    }
}
