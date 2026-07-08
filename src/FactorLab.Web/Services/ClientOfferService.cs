using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class ClientOfferService : IClientOfferService
{
    private readonly FactoringCalculator calculator;

    public ClientOfferService(FactoringCalculator calculator)
    {
        this.calculator = calculator;
    }

    public ClientOffer? CreateOffer(IPortfolioRepository portfolio, string clientName, string sentBy)
    {
        var invoices = portfolio.Invoices
            .Where(invoice => invoice.ClientName.Equals(clientName, StringComparison.OrdinalIgnoreCase))
            .Where(invoice => invoice.FundingStage is FundingStage.Submitted or FundingStage.Review or FundingStage.Approved)
            .Where(invoice => calculator.Decide(
                invoice,
                portfolio.Terms,
                portfolio.Clients.FirstOrDefault(client => client.Name == invoice.ClientName),
                portfolio.Debtors.FirstOrDefault(debtor => debtor.Name == invoice.Debtor),
                portfolio.Invoices).IsEligible)
            .ToArray();

        if (invoices.Length == 0) return null;

        var summary = calculator.Summarize(invoices, portfolio.Terms, portfolio.Clients, portfolio.Debtors);
        var offer = new ClientOffer
        {
            OfferNumber = $"OFR-{DateTime.UtcNow:yyyy}-{portfolio.ClientOffers.Count + 1:0000}",
            ClientName = clientName,
            SentToEmail = $"{Slug(clientName)}@client.example",
            InvoiceCount = invoices.Length,
            GrossReceivables = summary.EligibleAmount,
            AdvanceAmount = summary.Advance,
            Fees = summary.TotalCost,
            ReserveHeld = summary.Reserve,
            NetCash = summary.CashToday,
            WeightedDays = summary.WeightedDays,
            EffectiveApr = summary.EffectiveApr,
            InvoiceNumbers = string.Join(", ", invoices.Select(invoice => invoice.InvoiceNumber)),
            Notes = $"Prepared by {sentBy}. Subject to final document and debtor verification."
        };

        portfolio.ClientOffers.Add(offer);
        return offer;
    }

    public void Send(ClientOffer offer)
    {
        if (offer.Status != ClientOfferStatus.Draft) return;

        offer.Status = ClientOfferStatus.Sent;
    }

    public void Accept(IPortfolioRepository portfolio, ClientOffer offer, string acceptedBy)
    {
        if (!offer.IsActionable) return;

        offer.Status = ClientOfferStatus.Accepted;
        offer.AcceptedBy = acceptedBy;
        offer.AcceptedAt = DateTime.UtcNow;

        foreach (var invoiceNumber in ParseInvoiceNumbers(offer.InvoiceNumbers))
        {
            var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
            if (invoice is not null && invoice.FundingStage != FundingStage.Funded)
            {
                invoice.FundingStage = FundingStage.Approved;
            }
        }
    }

    public void Decline(ClientOffer offer, string declinedBy)
    {
        if (!offer.IsActionable) return;

        offer.Status = ClientOfferStatus.Declined;
        offer.Notes = string.IsNullOrWhiteSpace(offer.Notes)
            ? $"Declined by {declinedBy}."
            : $"{offer.Notes} Declined by {declinedBy}.";
    }

    private static IEnumerable<string> ParseInvoiceNumbers(string invoiceNumbers) =>
        invoiceNumbers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character == ' ')
            .Select(character => character == ' ' ? '.' : character)
            .ToArray();

        var slug = new string(chars).Trim('.');
        return string.IsNullOrWhiteSpace(slug) ? "client" : slug;
    }
}
