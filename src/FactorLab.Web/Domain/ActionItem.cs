namespace FactorLab.Web.Domain;

public sealed class ActionItem
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Detail { get; init; } = "";
    public string RelatedEntity { get; init; } = "";
    public UserRole OwnerRole { get; init; }
    public ActionItemPriority Priority { get; init; }
    public ActionItemCategory Category { get; init; }
    public DateTime DueAt { get; init; } = DateTime.UtcNow;
}
