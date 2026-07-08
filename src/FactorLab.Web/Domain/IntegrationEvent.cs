namespace FactorLab.Web.Domain;

public sealed class IntegrationEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = "";
    public string Entity { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Target { get; init; } = "";
    public IntegrationEventStatus Status { get; set; } = IntegrationEventStatus.Pending;
    public int Attempts { get; set; }
}
