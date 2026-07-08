using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class FraudSignalService : IFraudSignalService
{
    public IReadOnlyList<FraudSignal> Detect(IPortfolioRepository portfolio)
    {
        var signals = new List<FraudSignal>();
        AddDuplicateInvoiceSignals(signals, portfolio);
        AddLookalikeInvoiceSignals(signals, portfolio);
        AddUnknownDebtorSignals(signals, portfolio);
        AddDocumentSignals(signals, portfolio);
        AddStatusSignals(signals, portfolio);
        AddOverrideSignals(signals, portfolio);
        return signals
            .OrderByDescending(signal => signal.Severity)
            .ThenBy(signal => signal.Entity)
            .ToArray();
    }

    private static void AddDuplicateInvoiceSignals(List<FraudSignal> signals, IPortfolioRepository portfolio)
    {
        foreach (var group in portfolio.Invoices.GroupBy(invoice => invoice.InvoiceNumber).Where(group => group.Count() > 1))
        {
            signals.Add(new FraudSignal
            {
                SignalType = "Duplicate invoice number",
                Entity = group.Key,
                Severity = FraudSignalSeverity.Critical,
                Detail = $"{group.Count()} invoices share the same invoice number.",
                RecommendedAction = "Block funding until duplicate is resolved."
            });
        }
    }

    private static void AddLookalikeInvoiceSignals(List<FraudSignal> signals, IPortfolioRepository portfolio)
    {
        foreach (var group in portfolio.Invoices.GroupBy(invoice => new { invoice.Debtor, invoice.Amount, invoice.DaysToPay }).Where(group => group.Count() > 1))
        {
            signals.Add(new FraudSignal
            {
                SignalType = "Lookalike receivable",
                Entity = group.Key.Debtor,
                Severity = FraudSignalSeverity.High,
                Detail = $"{group.Count()} invoices have same debtor, amount and tenor.",
                RecommendedAction = "Request supporting documents and debtor confirmation."
            });
        }
    }

    private static void AddUnknownDebtorSignals(List<FraudSignal> signals, IPortfolioRepository portfolio)
    {
        foreach (var invoice in portfolio.Invoices.Where(invoice => portfolio.Debtors.All(debtor => debtor.Name != invoice.Debtor)))
        {
            signals.Add(new FraudSignal
            {
                SignalType = "Unknown debtor",
                Entity = invoice.InvoiceNumber,
                Severity = FraudSignalSeverity.High,
                Detail = $"{invoice.Debtor} has no debtor profile.",
                RecommendedAction = "Create debtor profile and credit limit before funding."
            });
        }
    }

    private static void AddDocumentSignals(List<FraudSignal> signals, IPortfolioRepository portfolio)
    {
        foreach (var invoice in portfolio.Invoices)
        {
            var debtorConfirmation = invoice.Documents.FirstOrDefault(document => document.Kind == DocumentKind.DebtorConfirmation);
            if (debtorConfirmation is null || !debtorConfirmation.IsSatisfied)
            {
                signals.Add(new FraudSignal
                {
                    SignalType = "Missing debtor confirmation",
                    Entity = invoice.InvoiceNumber,
                    Severity = invoice.FundingStage is FundingStage.Approved or FundingStage.Funded ? FraudSignalSeverity.High : FraudSignalSeverity.Medium,
                    Detail = $"{invoice.Debtor} has not confirmed the receivable.",
                    RecommendedAction = "Send confirmation request before funding."
                });
            }

            if (invoice.Amount >= 50000m && invoice.Amount % 10000m == 0m)
            {
                signals.Add(new FraudSignal
                {
                    SignalType = "Round high-value amount",
                    Entity = invoice.InvoiceNumber,
                    Severity = FraudSignalSeverity.Medium,
                    Detail = $"Invoice amount is a round high-value {invoice.Amount:0}.",
                    RecommendedAction = "Verify invoice and purchase order amounts."
                });
            }
        }
    }

    private static void AddStatusSignals(List<FraudSignal> signals, IPortfolioRepository portfolio)
    {
        foreach (var invoice in portfolio.Invoices.Where(invoice => invoice.Status is InvoiceStatus.Disputed or InvoiceStatus.Overdue))
        {
            signals.Add(new FraudSignal
            {
                SignalType = "Adverse invoice status",
                Entity = invoice.InvoiceNumber,
                Severity = invoice.Status == InvoiceStatus.Disputed ? FraudSignalSeverity.Critical : FraudSignalSeverity.High,
                Detail = $"{invoice.InvoiceNumber} status is {invoice.Status}.",
                RecommendedAction = "Exclude from funding or resolve before advance."
            });
        }
    }

    private static void AddOverrideSignals(List<FraudSignal> signals, IPortfolioRepository portfolio)
    {
        foreach (var decision in portfolio.UnderwritingDecisions.Where(decision => decision.IsManualOverride))
        {
            signals.Add(new FraudSignal
            {
                SignalType = "Manual override",
                Entity = decision.InvoiceNumber,
                Severity = FraudSignalSeverity.High,
                Detail = "Invoice was approved with manual override.",
                RecommendedAction = "Require second approval and audit note."
            });
        }
    }
}
