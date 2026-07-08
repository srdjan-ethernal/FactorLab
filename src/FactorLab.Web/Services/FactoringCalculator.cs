using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public sealed class FactoringCalculator
{
    public int ScoreInvoice(Invoice invoice, FactoringTerms terms)
    {
        var dayRisk = Math.Max(0, invoice.DaysToPay - 30) * 0.28m;
        var concentrationRisk = Math.Max(0, invoice.ConcentrationPercent - 25m) * 0.7m;
        var ratingRisk = invoice.Rating switch
        {
            DebtorRating.A => 4m,
            DebtorRating.B => 12m,
            DebtorRating.C => 24m,
            DebtorRating.D => 40m,
            _ => 25m
        };
        var statusRisk = invoice.Status switch
        {
            InvoiceStatus.Clean => 0m,
            InvoiceStatus.Unverified => 12m,
            InvoiceStatus.Disputed => 35m,
            InvoiceStatus.Overdue => 28m,
            _ => 0m
        };
        var modelAdjustment = terms.IsNonRecourse ? 5m : 0m;
        var risk = Math.Min(100m, Math.Round(ratingRisk + dayRisk + concentrationRisk + statusRisk + modelAdjustment));
        return Math.Max(0, 100 - (int)risk);
    }

    public EligibilityDecision Decide(Invoice invoice, FactoringTerms terms)
    {
        var policy = terms.RiskPolicy;
        var score = ScoreInvoice(invoice, terms);
        var blockers = new List<string>();
        var warnings = new List<string>();

        if (invoice.Amount <= 0) blockers.Add("Missing amount");
        if (invoice.DaysToPay > policy.MaximumTenorDays) blockers.Add($"Tenor above {policy.MaximumTenorDays} days");
        if (invoice.Status == InvoiceStatus.Disputed) blockers.Add("Disputed invoice");
        if (score < policy.MinimumScore) blockers.Add($"Risk score below {policy.MinimumScore} ({score})");

        if (invoice.Status == InvoiceStatus.Unverified) warnings.Add("Debtor confirmation needed");
        if (invoice.Status == InvoiceStatus.Overdue) warnings.Add("Overdue collection risk");
        if (invoice.DaysToPay > policy.LongTenorReviewDays && invoice.DaysToPay <= policy.MaximumTenorDays) warnings.Add("Long tenor review");
        if (invoice.ConcentrationPercent > policy.ConcentrationWarningPercent) warnings.Add("High debtor concentration");
        if (invoice.Rating == DebtorRating.D && score >= policy.MinimumScore) warnings.Add("Weak debtor rating");

        return new EligibilityDecision(blockers.Count == 0, blockers, warnings, score);
    }

    public EligibilityDecision Decide(
        Invoice invoice,
        FactoringTerms terms,
        ClientProfile? client,
        DebtorProfile? debtor,
        IReadOnlyCollection<Invoice> portfolio)
    {
        var baseDecision = Decide(invoice, terms);
        var blockers = baseDecision.Blockers.ToList();
        var warnings = baseDecision.Warnings.ToList();

        if (client is null)
        {
            warnings.Add("Client profile missing");
        }
        else
        {
            var clientExposure = ClientExposure(portfolio, client.Name);
            if (client.BuyDecision == BuyDecision.NoBuy) blockers.Add("Client marked no-buy");
            if (client.BuyDecision == BuyDecision.Watch) warnings.Add("Client on watchlist");
            if (client.KycStatus == KycStatus.Blocked) blockers.Add("Client KYC blocked");
            if (client.KycStatus == KycStatus.Pending) warnings.Add("Client KYC pending");
            if (client.KycStatus == KycStatus.RefreshRequired) warnings.Add("Client KYC refresh required");
            if (client.FacilityLimit > 0 && clientExposure > client.FacilityLimit) blockers.Add("Client facility limit exceeded");
            if (client.FacilityLimit > 0 && clientExposure / client.FacilityLimit * 100m >= terms.RiskPolicy.FacilityWarningUtilizationPercent) warnings.Add("Client facility near limit");
        }

        if (debtor is null)
        {
            warnings.Add("Debtor profile missing");
        }
        else
        {
            var debtorExposure = DebtorExposure(portfolio, debtor.Name);
            if (debtor.BuyDecision == BuyDecision.NoBuy) blockers.Add("Debtor marked no-buy");
            if (debtor.BuyDecision == BuyDecision.Watch) warnings.Add("Debtor on watchlist");
            if (debtor.CreditLimit > 0 && debtorExposure > debtor.CreditLimit) blockers.Add("Debtor credit limit exceeded");
            if (debtor.CreditLimit > 0 && debtorExposure / debtor.CreditLimit * 100m >= terms.RiskPolicy.CreditWarningUtilizationPercent) warnings.Add("Debtor credit near limit");
            if (debtor.DilutionPercent >= terms.RiskPolicy.HighDilutionPercent) warnings.Add("High dilution history");
            if (debtor.AverageDaysToPay > terms.RiskPolicy.SlowPaymentDays) warnings.Add("Slow debtor payment history");
        }

        return new EligibilityDecision(blockers.Count == 0, blockers.Distinct().ToArray(), warnings.Distinct().ToArray(), baseDecision.Score);
    }

    public decimal ClientExposure(IEnumerable<Invoice> portfolio, string clientName) =>
        portfolio.Where(invoice => invoice.ClientName == clientName && invoice.FundingStage != FundingStage.Settled)
            .Sum(invoice => invoice.Amount);

    public decimal DebtorExposure(IEnumerable<Invoice> portfolio, string debtorName) =>
        portfolio.Where(invoice => invoice.Debtor == debtorName && invoice.FundingStage != FundingStage.Settled)
            .Sum(invoice => invoice.Amount);

    public FactoringSummary Summarize(IReadOnlyCollection<Invoice> invoices, FactoringTerms terms)
    {
        return SummarizeCore(invoices, terms, invoice => Decide(invoice, terms));
    }

    public FactoringSummary Summarize(
        IReadOnlyCollection<Invoice> invoices,
        FactoringTerms terms,
        IReadOnlyCollection<ClientProfile> clients,
        IReadOnlyCollection<DebtorProfile> debtors)
    {
        return SummarizeCore(invoices, terms, invoice =>
            Decide(
                invoice,
                terms,
                clients.FirstOrDefault(client => client.Name == invoice.ClientName),
                debtors.FirstOrDefault(debtor => debtor.Name == invoice.Debtor),
                invoices));
    }

    private FactoringSummary SummarizeCore(IReadOnlyCollection<Invoice> invoices, FactoringTerms terms, Func<Invoice, EligibilityDecision> decide)
    {
        var decisions = invoices.Select(invoice => new { Invoice = invoice, Decision = decide(invoice) }).ToArray();
        var eligible = decisions.Where(item => item.Decision.IsEligible).Select(item => item.Invoice).ToArray();
        var eligibleAmount = eligible.Sum(invoice => invoice.Amount);
        var advance = eligibleAmount * terms.AdvanceRate / 100m;
        var reserve = eligibleAmount - advance;
        var weightedDays = eligibleAmount > 0
            ? eligible.Sum(invoice => invoice.Amount * invoice.DaysToPay) / eligibleAmount
            : 0m;
        var rawDiscount = eligible.Sum(invoice => invoice.Amount * terms.DiscountRatePer30Days / 100m * invoice.DaysToPay / 30m);
        var service = eligibleAmount * terms.ServiceFeeRate / 100m;
        var modelPremium = terms.IsNonRecourse ? eligibleAmount * 0.004m : 0m;
        var totalCost = eligibleAmount > 0 ? Math.Max(terms.MinimumFee, rawDiscount + service + modelPremium) : 0m;
        var cashToday = Math.Max(0m, advance - totalCost);
        var effectiveApr = advance > 0 && weightedDays > 0 ? totalCost / advance * (365m / weightedDays) * 100m : 0m;
        var bankCost = advance * terms.BankApr / 100m * weightedDays / 365m;
        var quality = invoices.Count > 0 ? (int)Math.Round(decisions.Average(item => item.Decision.Score)) : 0;
        var documentReadyCount = invoices.Count(invoice => invoice.Documents.Count > 0 && invoice.Documents.Where(document => document.IsRequired).All(document => document.IsSatisfied));
        var missingRequiredDocuments = invoices.Sum(invoice => invoice.Documents.Count(document => document.IsRequired && !document.IsSatisfied));

        return new FactoringSummary
        {
            EligibleAmount = eligibleAmount,
            Advance = advance,
            Reserve = reserve,
            TotalCost = totalCost,
            CashToday = cashToday,
            WeightedDays = weightedDays,
            EffectiveApr = effectiveApr,
            BankCost = bankCost,
            PortfolioQuality = quality,
            EligibleCount = eligible.Length,
            DocumentReadyCount = documentReadyCount,
            MissingRequiredDocuments = missingRequiredDocuments
        };
    }
}
