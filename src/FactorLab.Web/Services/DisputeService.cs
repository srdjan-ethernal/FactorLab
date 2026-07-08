using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class DisputeService : IDisputeService
{
    public DisputeCase Open(IPortfolioRepository portfolio, Invoice invoice, decimal disputedAmount, string reason, string owner)
    {
        var existing = portfolio.DisputeCases.FirstOrDefault(item =>
            item.InvoiceNumber == invoice.InvoiceNumber && item.Status is not DisputeStatus.Resolved and not DisputeStatus.ChargedBack);
        if (existing is not null) return existing;

        var dispute = new DisputeCase
        {
            CaseNumber = $"DSP-{DateTime.UtcNow:yyyy}-{portfolio.DisputeCases.Count + 1:0000}",
            InvoiceNumber = invoice.InvoiceNumber,
            DisputedAmount = disputedAmount,
            Reason = reason,
            Owner = owner,
            Status = DisputeStatus.Open,
            NextAction = "Collect evidence and debtor response."
        };

        invoice.Status = InvoiceStatus.Disputed;
        var collectionCase = portfolio.CollectionCaseFor(invoice.InvoiceNumber);
        collectionCase.Status = CollectionStatus.Overdue;
        collectionCase.NextAction = "Resolve dispute before payment follow-up";
        portfolio.DisputeCases.Add(dispute);
        portfolio.CollectionActivities.Add(new CollectionActivity
        {
            InvoiceNumber = invoice.InvoiceNumber,
            ActivityType = CollectionActivityType.Call,
            ContactName = invoice.Debtor,
            Note = $"Dispute opened: {reason}"
        });
        return dispute;
    }

    public void Resolve(IPortfolioRepository portfolio, DisputeCase dispute, decimal creditNoteAmount, bool chargeback)
    {
        var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber == dispute.InvoiceNumber);
        if (invoice is null) return;

        var collectionCase = portfolio.CollectionCaseFor(invoice.InvoiceNumber);
        dispute.CreditNoteAmount = Math.Max(0m, creditNoteAmount);
        dispute.ResolvedAt = DateTime.UtcNow;
        if (chargeback)
        {
            dispute.Status = DisputeStatus.ChargedBack;
            collectionCase.Status = CollectionStatus.Chargeback;
            collectionCase.ChargebackAmount = Math.Max(0m, dispute.DisputedAmount - dispute.CreditNoteAmount);
            collectionCase.NextAction = "Recover dispute balance from client";
        }
        else
        {
            dispute.Status = DisputeStatus.Resolved;
            invoice.Status = InvoiceStatus.Clean;
            collectionCase.NextAction = "Resume normal collection follow-up";
            if (collectionCase.Status == CollectionStatus.Chargeback)
            {
                collectionCase.Status = CollectionStatus.Overdue;
            }
        }

        portfolio.CollectionActivities.Add(new CollectionActivity
        {
            InvoiceNumber = invoice.InvoiceNumber,
            ActivityType = chargeback ? CollectionActivityType.Chargeback : CollectionActivityType.Email,
            ContactName = dispute.Owner,
            Note = chargeback
                ? $"Dispute charged back. Credit note: {dispute.CreditNoteAmount:0.00}."
                : $"Dispute resolved. Credit note: {dispute.CreditNoteAmount:0.00}."
        });
    }
}
