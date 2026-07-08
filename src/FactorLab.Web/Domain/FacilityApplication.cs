namespace FactorLab.Web.Domain;

public sealed class FacilityApplication
{
    public string ApplicationNumber { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Country { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public decimal RequestedLimit { get; set; }
    public decimal MonthlyTurnover { get; set; }
    public decimal AverageInvoiceSize { get; set; }
    public int ExpectedDebtorCount { get; set; } = 5;
    public int YearsTrading { get; set; } = 2;
    public FacilityApplicationStatus Status { get; set; } = FacilityApplicationStatus.Submitted;
    public int RiskScore { get; set; }
    public decimal ApprovedLimit { get; set; }
    public string DecisionNote { get; set; } = "";
    public string AssignedTo { get; set; } = "Unassigned";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string PortalToken { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    public bool IsActionable => Status is FacilityApplicationStatus.Submitted or FacilityApplicationStatus.InReview or FacilityApplicationStatus.MoreInfoRequired;
}
