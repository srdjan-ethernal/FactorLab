using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class FacilityApplicationService : IFacilityApplicationService
{
    public FacilityApplication Submit(IPortfolioRepository portfolio, FacilityApplication application)
    {
        application.ApplicationNumber = string.IsNullOrWhiteSpace(application.ApplicationNumber)
            ? $"APP-{DateTime.UtcNow:yyyy}-{portfolio.FacilityApplications.Count + 1:0000}"
            : application.ApplicationNumber;
        application.Status = FacilityApplicationStatus.Submitted;
        application.RiskScore = Score(application);
        application.ApprovedLimit = SuggestedLimit(application);
        application.SubmittedAt = DateTime.UtcNow;

        portfolio.FacilityApplications.Add(application);
        return application;
    }

    public void MoveToReview(FacilityApplication application, string assignedTo)
    {
        if (!application.IsActionable) return;

        application.Status = FacilityApplicationStatus.InReview;
        application.AssignedTo = assignedTo;
        application.ReviewedAt = DateTime.UtcNow;
        application.DecisionNote = "Application moved to underwriting review.";
    }

    public void RequestMoreInfo(FacilityApplication application, string assignedTo)
    {
        if (!application.IsActionable) return;

        application.Status = FacilityApplicationStatus.MoreInfoRequired;
        application.AssignedTo = assignedTo;
        application.ReviewedAt = DateTime.UtcNow;
        application.DecisionNote = "Request latest management accounts, ownership chart and top debtor schedule.";
    }

    public void Approve(IPortfolioRepository portfolio, FacilityApplication application, string approvedBy)
    {
        if (!application.IsActionable) return;

        application.Status = FacilityApplicationStatus.Approved;
        application.AssignedTo = approvedBy;
        application.ReviewedAt = DateTime.UtcNow;
        application.ApprovedLimit = application.ApprovedLimit <= 0m ? SuggestedLimit(application) : application.ApprovedLimit;
        application.DecisionNote = $"Approved for {application.ApprovedLimit:N0} subject to final KYC refresh and first debtor verification.";

        var client = portfolio.Clients.FirstOrDefault(item => item.Name.Equals(application.LegalName, StringComparison.OrdinalIgnoreCase));
        if (client is null)
        {
            portfolio.Clients.Add(new ClientProfile
            {
                Name = application.LegalName,
                Industry = application.Industry,
                Country = application.Country,
                FacilityLimit = application.ApprovedLimit,
                ConcentrationLimitPercent = application.ExpectedDebtorCount >= 5 ? 35m : 25m,
                KycStatus = KycStatus.Approved,
                BuyDecision = application.RiskScore >= 72 ? BuyDecision.Buy : BuyDecision.Watch,
                AccountManager = approvedBy
            });
            return;
        }

        client.FacilityLimit = application.ApprovedLimit;
        client.Industry = application.Industry;
        client.Country = application.Country;
        client.KycStatus = KycStatus.Approved;
        client.BuyDecision = application.RiskScore >= 72 ? BuyDecision.Buy : BuyDecision.Watch;
        client.AccountManager = approvedBy;
    }

    public void Decline(FacilityApplication application, string declinedBy)
    {
        if (!application.IsActionable) return;

        application.Status = FacilityApplicationStatus.Declined;
        application.AssignedTo = declinedBy;
        application.ReviewedAt = DateTime.UtcNow;
        application.DecisionNote = "Declined under current risk appetite.";
    }

    private static int Score(FacilityApplication application)
    {
        var score = 100m;
        if (application.YearsTrading < 1) score -= 24m;
        if (application.YearsTrading is >= 1 and < 3) score -= 10m;
        if (application.ExpectedDebtorCount < 3) score -= 18m;
        if (application.ExpectedDebtorCount is >= 3 and < 5) score -= 8m;
        if (application.MonthlyTurnover <= 0m) score -= 30m;
        if (application.RequestedLimit > application.MonthlyTurnover * 2.5m) score -= 18m;
        if (application.AverageInvoiceSize > 0m && application.RequestedLimit / application.AverageInvoiceSize < 3m) score -= 8m;
        if (application.Country.Equals("Serbia", StringComparison.OrdinalIgnoreCase)) score += 3m;
        if (application.Industry.Contains("construction", StringComparison.OrdinalIgnoreCase)) score -= 8m;

        return (int)Math.Clamp(Math.Round(score), 0m, 100m);
    }

    private static decimal SuggestedLimit(FacilityApplication application)
    {
        var turnoverBased = application.MonthlyTurnover * 1.6m;
        var debtorBased = application.AverageInvoiceSize * Math.Max(3, application.ExpectedDebtorCount);
        var riskHaircut = application.RiskScore >= 75 ? 1m : application.RiskScore >= 60 ? 0.75m : 0.5m;
        return Math.Round(Math.Min(application.RequestedLimit, Math.Min(turnoverBased, debtorBased)) * riskHaircut, 0);
    }
}
