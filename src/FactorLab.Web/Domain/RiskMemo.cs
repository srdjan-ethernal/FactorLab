namespace FactorLab.Web.Domain;

public sealed class RiskMemo
{
    public string InvoiceNumber { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string ExecutiveSummary { get; set; } = "";
    public List<string> Strengths { get; } = new();
    public List<string> Risks { get; } = new();
    public List<string> Conditions { get; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
