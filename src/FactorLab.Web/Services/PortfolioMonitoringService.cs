using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class PortfolioMonitoringService : IPortfolioMonitoringService
{
    private readonly FactoringCalculator calculator;

    public PortfolioMonitoringService(FactoringCalculator calculator)
    {
        this.calculator = calculator;
    }

    public IReadOnlyList<CovenantCheck> BuildChecks(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        var checks = new List<CovenantCheck>();
        AddDocumentReadiness(checks, portfolio);
        AddClientFacilityChecks(checks, portfolio, terms);
        AddDebtorLimitChecks(checks, portfolio, terms);
        AddKycChecks(checks, portfolio);
        AddCollectionsChecks(checks, portfolio);
        return checks
            .OrderByDescending(item => item.Status)
            .ThenBy(item => item.Scope)
            .ToArray();
    }

    private static void AddDocumentReadiness(List<CovenantCheck> checks, IPortfolioRepository portfolio)
    {
        var invoiceCount = Math.Max(1, portfolio.Invoices.Count);
        var readyCount = portfolio.Invoices.Count(invoice =>
            invoice.Documents.Count > 0 && invoice.Documents.Where(document => document.IsRequired).All(document => document.IsSatisfied));
        var readiness = readyCount * 100m / invoiceCount;
        checks.Add(new CovenantCheck
        {
            Name = "Document readiness",
            Scope = "Portfolio",
            Status = readiness >= 80m ? CovenantStatus.Pass : readiness >= 60m ? CovenantStatus.Watch : CovenantStatus.Breach,
            Metric = $"{readiness:0}% ready",
            Threshold = "80% target",
            Action = readiness >= 80m ? "Maintain upload discipline" : "Complete required document packs before funding"
        });
    }

    private void AddClientFacilityChecks(List<CovenantCheck> checks, IPortfolioRepository portfolio, FactoringTerms terms)
    {
        foreach (var client in portfolio.Clients)
        {
            var exposure = calculator.ClientExposure(portfolio.Invoices, client.Name);
            var utilization = client.FacilityLimit <= 0 ? 0m : exposure / client.FacilityLimit * 100m;
            checks.Add(new CovenantCheck
            {
                Name = "Client facility utilization",
                Scope = client.Name,
                Status = utilization > 100m ? CovenantStatus.Breach : utilization >= terms.RiskPolicy.FacilityWarningUtilizationPercent ? CovenantStatus.Watch : CovenantStatus.Pass,
                Metric = $"{utilization:0}% used",
                Threshold = $"{terms.RiskPolicy.FacilityWarningUtilizationPercent:0}% watch / 100% breach",
                Action = utilization > 100m ? "Stop new funding or raise facility limit" : "Monitor exposure before next submission"
            });

            var maxConcentration = portfolio.Invoices
                .Where(invoice => invoice.ClientName == client.Name)
                .Select(invoice => invoice.ConcentrationPercent)
                .DefaultIfEmpty(0m)
                .Max();
            checks.Add(new CovenantCheck
            {
                Name = "Debtor concentration",
                Scope = client.Name,
                Status = maxConcentration > client.ConcentrationLimitPercent + 10m ? CovenantStatus.Breach : maxConcentration > client.ConcentrationLimitPercent ? CovenantStatus.Watch : CovenantStatus.Pass,
                Metric = $"{maxConcentration:0}% max",
                Threshold = $"{client.ConcentrationLimitPercent:0}% policy",
                Action = maxConcentration > client.ConcentrationLimitPercent ? "Diversify debtor exposure or require approval" : "Within concentration policy"
            });
        }
    }

    private void AddDebtorLimitChecks(List<CovenantCheck> checks, IPortfolioRepository portfolio, FactoringTerms terms)
    {
        foreach (var debtor in portfolio.Debtors)
        {
            var exposure = calculator.DebtorExposure(portfolio.Invoices, debtor.Name);
            var utilization = debtor.CreditLimit <= 0 ? 0m : exposure / debtor.CreditLimit * 100m;
            checks.Add(new CovenantCheck
            {
                Name = "Debtor credit utilization",
                Scope = debtor.Name,
                Status = utilization > 100m ? CovenantStatus.Breach : utilization >= terms.RiskPolicy.CreditWarningUtilizationPercent ? CovenantStatus.Watch : CovenantStatus.Pass,
                Metric = $"{utilization:0}% used",
                Threshold = $"{terms.RiskPolicy.CreditWarningUtilizationPercent:0}% watch / 100% breach",
                Action = utilization > 100m ? "Block additional debtor exposure" : "Monitor debtor limit before funding"
            });
        }
    }

    private static void AddKycChecks(List<CovenantCheck> checks, IPortfolioRepository portfolio)
    {
        foreach (var client in portfolio.Clients.Where(client => client.KycStatus != KycStatus.Approved))
        {
            checks.Add(new CovenantCheck
            {
                Name = "KYC status",
                Scope = client.Name,
                Status = client.KycStatus == KycStatus.Blocked ? CovenantStatus.Breach : CovenantStatus.Watch,
                Metric = client.KycStatus.ToString(),
                Threshold = "Approved",
                Action = client.KycStatus == KycStatus.Blocked ? "Do not fund until KYC is cleared" : "Collect updated KYC evidence"
            });
        }
    }

    private static void AddCollectionsChecks(List<CovenantCheck> checks, IPortfolioRepository portfolio)
    {
        var overdue = portfolio.CollectionCases.Count(item => item.DaysPastDue > 0 && item.Status != CollectionStatus.Paid);
        var chargebacks = portfolio.CollectionCases.Count(item => item.Status == CollectionStatus.Chargeback);
        checks.Add(new CovenantCheck
        {
            Name = "Collections exceptions",
            Scope = "Portfolio",
            Status = chargebacks > 0 ? CovenantStatus.Breach : overdue > 0 ? CovenantStatus.Watch : CovenantStatus.Pass,
            Metric = $"{overdue} overdue / {chargebacks} chargeback",
            Threshold = "0 chargebacks",
            Action = chargebacks > 0 ? "Escalate recovery and reserve impact" : overdue > 0 ? "Prioritize collection follow-up" : "No collection exception"
        });
    }
}
