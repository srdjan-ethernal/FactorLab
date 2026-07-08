using System.Globalization;
using System.Text;
using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class CsvReportExportService : IReportExportService
{
    private readonly FactoringCalculator calculator;
    private readonly IFundingLedgerService ledger;
    private readonly IBorrowingBaseService borrowingBase;
    private readonly IFraudSignalService fraudSignals;

    public CsvReportExportService(FactoringCalculator calculator, IFundingLedgerService ledger, IBorrowingBaseService borrowingBase, IFraudSignalService fraudSignals)
    {
        this.calculator = calculator;
        this.ledger = ledger;
        this.borrowingBase = borrowingBase;
        this.fraudSignals = fraudSignals;
    }

    public ReportExport Export(ReportKind kind, IPortfolioRepository portfolio, FactoringTerms terms)
    {
        return kind switch
        {
            ReportKind.Exposure => Build("factorlab-exposure.csv", ExposureRows(portfolio)),
            ReportKind.Collections => Build("factorlab-collections.csv", CollectionRows(portfolio)),
            ReportKind.Underwriting => Build("factorlab-underwriting.csv", UnderwritingRows(portfolio, terms)),
            ReportKind.Ledger => Build("factorlab-ledger.csv", LedgerRows(portfolio, terms)),
            ReportKind.BorrowingBase => Build("factorlab-borrowing-base.csv", BorrowingBaseRows(portfolio, terms)),
            ReportKind.Applications => Build("factorlab-facility-applications.csv", ApplicationRows(portfolio)),
            ReportKind.Payments => Build("factorlab-payments.csv", PaymentRows(portfolio)),
            ReportKind.Disputes => Build("factorlab-disputes.csv", DisputeRows(portfolio)),
            ReportKind.Confirmations => Build("factorlab-confirmations.csv", ConfirmationRows(portfolio)),
            ReportKind.Fraud => Build("factorlab-fraud-signals.csv", FraudRows(portfolio)),
            _ => Build("factorlab-portfolio.csv", PortfolioRows(portfolio, terms))
        };
    }

    private ReportExport Build(string fileName, IEnumerable<string[]> rows)
    {
        var csv = new StringBuilder();
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", row.Select(Escape)));
        }

        return new ReportExport
        {
            FileName = fileName,
            Content = csv.ToString()
        };
    }

    private IEnumerable<string[]> PortfolioRows(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        yield return new[] { "Client", "Debtor", "Invoice", "Amount", "Stage", "Score", "Eligible", "DocumentsReady", "CashToday" };
        foreach (var invoice in portfolio.Invoices)
        {
            var decision = calculator.Decide(
                invoice,
                terms,
                portfolio.Clients.FirstOrDefault(client => client.Name == invoice.ClientName),
                portfolio.Debtors.FirstOrDefault(debtor => debtor.Name == invoice.Debtor),
                portfolio.Invoices);
            var documentsReady = invoice.Documents.Count > 0 && invoice.Documents.Where(document => document.IsRequired).All(document => document.IsSatisfied);
            var cashToday = decision.IsEligible ? invoice.Amount * terms.AdvanceRate / 100m : 0m;
            yield return new[]
            {
                invoice.ClientName,
                invoice.Debtor,
                invoice.InvoiceNumber,
                Money(invoice.Amount),
                invoice.FundingStage.ToString(),
                decision.Score.ToString(CultureInfo.InvariantCulture),
                decision.IsEligible ? "Yes" : "No",
                documentsReady ? "Yes" : "No",
                Money(cashToday)
            };
        }
    }

    private IEnumerable<string[]> ExposureRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "Type", "Name", "Limit", "Exposure", "Utilization", "Decision", "Status" };
        foreach (var client in portfolio.Clients)
        {
            var exposure = calculator.ClientExposure(portfolio.Invoices, client.Name);
            yield return new[]
            {
                "Client",
                client.Name,
                Money(client.FacilityLimit),
                Money(exposure),
                Percent(exposure, client.FacilityLimit),
                client.BuyDecision.ToString(),
                client.KycStatus.ToString()
            };
        }

        foreach (var debtor in portfolio.Debtors)
        {
            var exposure = calculator.DebtorExposure(portfolio.Invoices, debtor.Name);
            yield return new[]
            {
                "Debtor",
                debtor.Name,
                Money(debtor.CreditLimit),
                Money(exposure),
                Percent(exposure, debtor.CreditLimit),
                debtor.BuyDecision.ToString(),
                debtor.Rating.ToString()
            };
        }
    }

    private IEnumerable<string[]> CollectionRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "Invoice", "Debtor", "DueDate", "Status", "DaysPastDue", "Amount", "Paid", "ReserveReleased", "Chargeback", "NextAction" };
        foreach (var collectionCase in portfolio.CollectionCases)
        {
            var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber == collectionCase.InvoiceNumber);
            yield return new[]
            {
                collectionCase.InvoiceNumber,
                invoice?.Debtor ?? "",
                collectionCase.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                collectionCase.Status.ToString(),
                collectionCase.DaysPastDue.ToString(CultureInfo.InvariantCulture),
                Money(invoice?.Amount ?? 0m),
                Money(collectionCase.AmountPaid),
                Money(collectionCase.ReserveReleased),
                Money(collectionCase.ChargebackAmount),
                collectionCase.NextAction
            };
        }
    }

    private IEnumerable<string[]> UnderwritingRows(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        yield return new[] { "Invoice", "Client", "Debtor", "Decision", "AssignedTo", "Score", "Blockers", "Warnings", "Conditions" };
        foreach (var underwriting in portfolio.UnderwritingDecisions)
        {
            var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber == underwriting.InvoiceNumber);
            if (invoice is null) continue;

            var decision = calculator.Decide(
                invoice,
                terms,
                portfolio.Clients.FirstOrDefault(client => client.Name == invoice.ClientName),
                portfolio.Debtors.FirstOrDefault(debtor => debtor.Name == invoice.Debtor),
                portfolio.Invoices);

            yield return new[]
            {
                invoice.InvoiceNumber,
                invoice.ClientName,
                invoice.Debtor,
                underwriting.Status.ToString(),
                underwriting.AssignedTo,
                decision.Score.ToString(CultureInfo.InvariantCulture),
                string.Join("; ", decision.Blockers),
                string.Join("; ", decision.Warnings),
                underwriting.Conditions
            };
        }
    }

    private IEnumerable<string[]> LedgerRows(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        yield return new[] { "PostedAt", "Invoice", "Client", "Debtor", "EntryType", "Debit", "Credit", "Currency", "Note" };
        foreach (var entry in ledger.BuildLedger(portfolio, terms))
        {
            yield return new[]
            {
                entry.PostedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                entry.InvoiceNumber,
                entry.ClientName,
                entry.Debtor,
                entry.EntryType.ToString(),
                Money(entry.Debit),
                Money(entry.Credit),
                entry.Currency,
                entry.Note
            };
        }
    }

    private IEnumerable<string[]> BorrowingBaseRows(IPortfolioRepository portfolio, FactoringTerms terms)
    {
        yield return new[] { "Client", "FacilityLimit", "GrossReceivables", "EligibleReceivables", "IneligibleReceivables", "ConcentrationExcess", "DebtorLimitExcess", "BorrowingBase", "MaxAdvance", "ExistingAdvance", "Availability", "Status" };
        foreach (var line in borrowingBase.Build(portfolio, terms))
        {
            yield return new[]
            {
                line.ClientName,
                Money(line.FacilityLimit),
                Money(line.GrossReceivables),
                Money(line.EligibleReceivables),
                Money(line.IneligibleReceivables),
                Money(line.ConcentrationExcess),
                Money(line.DebtorLimitExcess),
                Money(line.BorrowingBase),
                Money(line.MaxAdvance),
                Money(line.ExistingAdvance),
                Money(line.Availability),
                line.Status
            };
        }
    }

    private static IEnumerable<string[]> ApplicationRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "ApplicationNumber", "LegalName", "Industry", "Country", "ContactEmail", "RequestedLimit", "MonthlyTurnover", "AverageInvoiceSize", "ExpectedDebtorCount", "YearsTrading", "Status", "RiskScore", "ApprovedLimit", "AssignedTo", "DecisionNote", "SubmittedAt", "ReviewedAt" };
        foreach (var application in portfolio.FacilityApplications)
        {
            yield return new[]
            {
                application.ApplicationNumber,
                application.LegalName,
                application.Industry,
                application.Country,
                application.ContactEmail,
                Money(application.RequestedLimit),
                Money(application.MonthlyTurnover),
                Money(application.AverageInvoiceSize),
                application.ExpectedDebtorCount.ToString(CultureInfo.InvariantCulture),
                application.YearsTrading.ToString(CultureInfo.InvariantCulture),
                application.Status.ToString(),
                application.RiskScore.ToString(CultureInfo.InvariantCulture),
                Money(application.ApprovedLimit),
                application.AssignedTo,
                application.DecisionNote,
                application.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                application.ReviewedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""
            };
        }
    }

    private static IEnumerable<string[]> PaymentRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "ReceivedAt", "Reference", "Invoice", "Debtor", "Amount", "Currency", "Status", "Note" };
        foreach (var match in portfolio.PaymentMatches)
        {
            yield return new[]
            {
                match.ReceivedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                match.Reference,
                match.InvoiceNumber,
                match.Debtor,
                Money(match.Amount),
                match.Currency,
                match.Status.ToString(),
                match.Note
            };
        }
    }

    private static IEnumerable<string[]> DisputeRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "CaseNumber", "Invoice", "OpenedAt", "Status", "DisputedAmount", "CreditNoteAmount", "Reason", "Owner", "NextAction" };
        foreach (var dispute in portfolio.DisputeCases)
        {
            yield return new[]
            {
                dispute.CaseNumber,
                dispute.InvoiceNumber,
                dispute.OpenedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                dispute.Status.ToString(),
                Money(dispute.DisputedAmount),
                Money(dispute.CreditNoteAmount),
                dispute.Reason,
                dispute.Owner,
                dispute.NextAction
            };
        }
    }

    private static IEnumerable<string[]> ConfirmationRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "RequestNumber", "Invoice", "Debtor", "CreatedAt", "SentAt", "DueAt", "Status", "SentBy", "ResponseNote", "RespondedAt" };
        foreach (var request in portfolio.DebtorConfirmations)
        {
            yield return new[]
            {
                request.RequestNumber,
                request.InvoiceNumber,
                request.Debtor,
                request.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                request.SentAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                request.DueAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                request.Status.ToString(),
                request.SentBy,
                request.ResponseNote,
                request.RespondedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""
            };
        }
    }

    private IEnumerable<string[]> FraudRows(IPortfolioRepository portfolio)
    {
        yield return new[] { "SignalType", "Entity", "Severity", "Detail", "RecommendedAction" };
        foreach (var signal in fraudSignals.Detect(portfolio))
        {
            yield return new[]
            {
                signal.SignalType,
                signal.Entity,
                signal.Severity.ToString(),
                signal.Detail,
                signal.RecommendedAction
            };
        }
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Percent(decimal numerator, decimal denominator) => denominator <= 0 ? "0%" : $"{numerator / denominator * 100m:0.0}%";
}
