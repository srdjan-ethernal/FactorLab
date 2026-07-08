using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class DebtorConfirmationService : IDebtorConfirmationService
{
    public DebtorConfirmationRequest Send(IPortfolioRepository portfolio, Invoice invoice, string sentBy)
    {
        var existing = portfolio.DebtorConfirmations.FirstOrDefault(item =>
            item.InvoiceNumber == invoice.InvoiceNumber && item.Status is DebtorConfirmationStatus.Draft or DebtorConfirmationStatus.Sent);
        if (existing is not null) return existing;

        var request = new DebtorConfirmationRequest
        {
            RequestNumber = $"DCF-{DateTime.UtcNow:yyyy}-{portfolio.DebtorConfirmations.Count + 1:0000}",
            InvoiceNumber = invoice.InvoiceNumber,
            Debtor = invoice.Debtor,
            SentAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.Date.AddDays(3),
            Status = DebtorConfirmationStatus.Sent,
            SentBy = sentBy,
            ResponseNote = "Awaiting debtor response."
        };
        portfolio.DebtorConfirmations.Add(request);
        portfolio.CollectionActivities.Add(new CollectionActivity
        {
            InvoiceNumber = invoice.InvoiceNumber,
            ActivityType = CollectionActivityType.Email,
            ContactName = invoice.Debtor,
            Note = "Debtor confirmation request sent."
        });
        return request;
    }

    public void Confirm(IPortfolioRepository portfolio, DebtorConfirmationRequest request, string note)
    {
        request.Status = DebtorConfirmationStatus.Confirmed;
        request.ResponseNote = note;
        request.RespondedAt = DateTime.UtcNow;

        var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber == request.InvoiceNumber);
        var document = invoice?.Documents.FirstOrDefault(item => item.Kind == DocumentKind.DebtorConfirmation);
        if (document is not null)
        {
            document.Status = DocumentStatus.Verified;
            document.ReceivedAt = DateTime.UtcNow;
            document.FileName = "debtor-confirmation.txt";
            document.ReviewerNote = note;
        }
    }

    public void Dispute(IPortfolioRepository portfolio, DebtorConfirmationRequest request, string note, IDisputeService disputeService, string owner)
    {
        request.Status = DebtorConfirmationStatus.Disputed;
        request.ResponseNote = note;
        request.RespondedAt = DateTime.UtcNow;

        var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber == request.InvoiceNumber);
        if (invoice is not null)
        {
            disputeService.Open(portfolio, invoice, invoice.Amount, note, owner);
        }
    }
}
