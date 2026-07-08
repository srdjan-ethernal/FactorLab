using System.Globalization;
using System.Text;
using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class LocalTemplateGenerationService : ITemplateGenerationService
{
    private readonly FactoringCalculator calculator;

    public LocalTemplateGenerationService(FactoringCalculator calculator)
    {
        this.calculator = calculator;
    }

    public GeneratedTemplate Generate(TemplateKind kind, Invoice invoice, IPortfolioRepository portfolio, FactoringTerms terms)
    {
        var client = portfolio.Clients.FirstOrDefault(item => item.Name == invoice.ClientName);
        var debtor = portfolio.Debtors.FirstOrDefault(item => item.Name == invoice.Debtor);
        var decision = calculator.Decide(invoice, terms, client, debtor, portfolio.Invoices);
        var content = kind switch
        {
            TemplateKind.NoticeOfAssignment => NoticeOfAssignment(invoice, client, terms),
            TemplateKind.DebtorConfirmation => DebtorConfirmation(invoice, client),
            TemplateKind.CollectionReminder => CollectionReminder(invoice, portfolio.CollectionCaseFor(invoice.InvoiceNumber), terms),
            _ => FundingOffer(invoice, client, debtor, decision, terms)
        };

        return new GeneratedTemplate
        {
            FileName = $"factorlab-{kind.ToString().ToLowerInvariant()}-{invoice.InvoiceNumber}.txt",
            Content = content
        };
    }

    private static string FundingOffer(Invoice invoice, ClientProfile? client, DebtorProfile? debtor, EligibilityDecision decision, FactoringTerms terms)
    {
        var advance = invoice.Amount * terms.AdvanceRate / 100m;
        var reserve = invoice.Amount - advance;
        var fee = Math.Max(terms.MinimumFee, invoice.Amount * terms.DiscountRatePer30Days / 100m * invoice.DaysToPay / 30m + invoice.Amount * terms.ServiceFeeRate / 100m);
        var lines = new[]
        {
            "INDICATIVE FUNDING OFFER",
            "",
            $"Client: {invoice.ClientName}",
            $"Debtor: {invoice.Debtor}",
            $"Invoice: {invoice.InvoiceNumber}",
            $"Invoice amount: {Money(invoice.Amount, terms.Currency)}",
            $"Advance rate: {terms.AdvanceRate:0.##}%",
            $"Gross advance: {Money(advance, terms.Currency)}",
            $"Estimated fee: {Money(fee, terms.Currency)}",
            $"Reserve: {Money(reserve, terms.Currency)}",
            $"Expected tenor: {invoice.DaysToPay} days",
            $"Eligibility score: {decision.Score}",
            $"Recommendation: {(decision.IsEligible ? "Proceed subject to verification" : "Do not fund until blockers are cleared")}",
            "",
            "Conditions:",
            "- Valid invoice and supporting commercial documents",
            "- Debtor confirmation and no undisclosed disputes or set-offs",
            "- Notice of assignment sent before funding",
            "- Final approval subject to KYC, limits and underwriting review",
            "",
            $"Client KYC: {client?.KycStatus.ToString() ?? "Missing profile"}",
            $"Debtor rating: {debtor?.Rating.ToString() ?? "Missing profile"}"
        };
        return string.Join(Environment.NewLine, lines);
    }

    private static string NoticeOfAssignment(Invoice invoice, ClientProfile? client, FactoringTerms terms)
    {
        var lines = new[]
        {
            "NOTICE OF ASSIGNMENT",
            "",
            $"Date: {DateTime.UtcNow:yyyy-MM-dd}",
            $"To: {invoice.Debtor}",
            $"Re: Invoice {invoice.InvoiceNumber} issued by {invoice.ClientName}",
            "",
            $"Please be advised that {invoice.ClientName} has assigned the receivable identified above to FactorLab for financing and collection administration.",
            $"The current invoice amount is {Money(invoice.Amount, terms.Currency)} with expected payment in {invoice.DaysToPay} days.",
            "",
            "Please direct payment and remittance advice according to the payment instructions provided by FactorLab.",
            "Payment to the original seller after receipt of this notice may not discharge the obligation.",
            "",
            $"Seller contact: {client?.AccountManager ?? invoice.ClientName}",
            "",
            "This notice is generated for workflow preparation and should be reviewed by counsel before external delivery."
        };
        return string.Join(Environment.NewLine, lines);
    }

    private static string DebtorConfirmation(Invoice invoice, ClientProfile? client)
    {
        var lines = new[]
        {
            "DEBTOR CONFIRMATION REQUEST",
            "",
            $"To: {invoice.Debtor}",
            $"Invoice: {invoice.InvoiceNumber}",
            $"Supplier: {invoice.ClientName}",
            $"Amount: {invoice.Amount.ToString("0.00", CultureInfo.InvariantCulture)}",
            "",
            "Please confirm the following:",
            "- Goods or services have been received and accepted.",
            "- The invoice is valid, payable and not subject to dispute.",
            "- There are no set-offs, credits, returns or counterclaims currently known.",
            "- Expected payment date remains consistent with the commercial terms.",
            "",
            $"Supplier contact: {client?.AccountManager ?? invoice.ClientName}",
            "",
            "Reply with confirmed, disputed, or requires clarification."
        };
        return string.Join(Environment.NewLine, lines);
    }

    private static string CollectionReminder(Invoice invoice, CollectionCase collectionCase, FactoringTerms terms)
    {
        var lines = new[]
        {
            "COLLECTION REMINDER",
            "",
            $"To: {invoice.Debtor}",
            $"Invoice: {invoice.InvoiceNumber}",
            $"Amount due: {Money(invoice.Amount - collectionCase.AmountPaid, terms.Currency)}",
            $"Due date: {collectionCase.DueDate:yyyy-MM-dd}",
            $"Days past due: {collectionCase.DaysPastDue}",
            "",
            "Our records show that the above invoice remains open.",
            "Please confirm payment status, expected payment date, and any remittance reference.",
            "",
            $"Current next action: {collectionCase.NextAction}",
            "",
            "If payment has already been made, please send remittance advice."
        };
        return string.Join(Environment.NewLine, lines);
    }

    private static string Money(decimal value, string currency) =>
        $"{currency} {value.ToString("N2", CultureInfo.InvariantCulture)}";
}
