using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class LocalRiskMemoService : IRiskMemoService
{
    private readonly FactoringCalculator calculator;

    public LocalRiskMemoService(FactoringCalculator calculator)
    {
        this.calculator = calculator;
    }

    public RiskMemo Generate(Invoice invoice, IPortfolioRepository portfolio, FactoringTerms terms)
    {
        var client = portfolio.Clients.FirstOrDefault(item => item.Name == invoice.ClientName);
        var debtor = portfolio.Debtors.FirstOrDefault(item => item.Name == invoice.Debtor);
        var decision = calculator.Decide(invoice, terms, client, debtor, portfolio.Invoices);
        var underwriting = portfolio.DecisionFor(invoice.InvoiceNumber);
        var collectionCase = portfolio.CollectionCaseFor(invoice.InvoiceNumber);
        var clientExposure = client is null ? 0m : calculator.ClientExposure(portfolio.Invoices, client.Name);
        var debtorExposure = debtor is null ? 0m : calculator.DebtorExposure(portfolio.Invoices, debtor.Name);
        var requiredDocs = invoice.Documents.Where(document => document.IsRequired).ToArray();
        var missingDocs = requiredDocs.Count(document => !document.IsSatisfied);

        var memo = new RiskMemo
        {
            InvoiceNumber = invoice.InvoiceNumber,
            Recommendation = Recommendation(decision, underwriting, missingDocs, collectionCase),
            ExecutiveSummary = BuildSummary(invoice, decision, underwriting, collectionCase, missingDocs)
        };

        if (decision.Score >= 75) memo.Strengths.Add($"Strong eligibility score ({decision.Score}).");
        if (client is not null && client.KycStatus == KycStatus.Approved) memo.Strengths.Add("Client KYC is approved.");
        if (debtor is not null && debtor.BuyDecision == BuyDecision.Buy) memo.Strengths.Add("Debtor is currently buy-approved.");
        if (missingDocs == 0) memo.Strengths.Add("Required document checklist is complete.");
        if (collectionCase.Status == CollectionStatus.Paid) memo.Strengths.Add("Payment has been received and matched.");

        foreach (var blocker in decision.Blockers) memo.Risks.Add(blocker);
        foreach (var warning in decision.Warnings) memo.Risks.Add(warning);
        if (client is not null && client.FacilityLimit > 0) memo.Risks.Add($"Client exposure is {Percent(clientExposure, client.FacilityLimit)} of facility.");
        if (debtor is not null && debtor.CreditLimit > 0) memo.Risks.Add($"Debtor exposure is {Percent(debtorExposure, debtor.CreditLimit)} of credit limit.");
        if (collectionCase.DaysPastDue > 0 && collectionCase.Status != CollectionStatus.Paid) memo.Risks.Add($"Collection is {collectionCase.DaysPastDue} days past due.");

        if (missingDocs > 0) memo.Conditions.Add($"Complete {missingDocs} missing required document(s).");
        if (!string.IsNullOrWhiteSpace(underwriting.Conditions)) memo.Conditions.Add(underwriting.Conditions);
        if (debtor is not null && debtor.BuyDecision == BuyDecision.Watch) memo.Conditions.Add("Senior underwriter review recommended for watchlist debtor.");
        if (client is not null && client.KycStatus == KycStatus.RefreshRequired) memo.Conditions.Add("Refresh client KYC before increasing exposure.");

        if (memo.Strengths.Count == 0) memo.Strengths.Add("No positive risk mitigants identified yet.");
        if (memo.Risks.Count == 0) memo.Risks.Add("No material risk flags detected.");
        if (memo.Conditions.Count == 0) memo.Conditions.Add("No additional conditions proposed.");

        return memo;
    }

    private static string Recommendation(EligibilityDecision decision, UnderwritingDecision underwriting, int missingDocs, CollectionCase collectionCase)
    {
        if (underwriting.Status == UnderwritingDecisionStatus.OverrideApproved) return "Override approved";
        if (!decision.IsEligible) return "Decline or remediate blockers";
        if (collectionCase.Status == CollectionStatus.Chargeback) return "Decline until chargeback is resolved";
        if (missingDocs > 0) return "Approve with conditions";
        if (decision.Warnings.Count > 0) return "Approve with monitoring";
        return "Approve";
    }

    private static string BuildSummary(Invoice invoice, EligibilityDecision decision, UnderwritingDecision underwriting, CollectionCase collectionCase, int missingDocs)
    {
        return $"{invoice.InvoiceNumber} for {invoice.Debtor} has score {decision.Score}, underwriting status {underwriting.Status}, collection status {collectionCase.Status}, and {missingDocs} missing required document(s).";
    }

    private static string Percent(decimal numerator, decimal denominator)
    {
        if (denominator <= 0) return "0%";
        return $"{numerator / denominator * 100m:0}%";
    }
}
