using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class LocalActionCenterService : IActionCenterService
{
    private readonly FactoringCalculator calculator;

    public LocalActionCenterService(FactoringCalculator calculator)
    {
        this.calculator = calculator;
    }

    public IReadOnlyList<ActionItem> BuildQueue(IPortfolioRepository portfolio, FactoringTerms terms, IReadOnlyCollection<string> completedActionIds)
    {
        var completed = completedActionIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var items = new List<ActionItem>();

        foreach (var client in portfolio.Clients.Where(client => client.KycStatus == KycStatus.RefreshRequired || client.KycStatus == KycStatus.Pending))
        {
            items.Add(new ActionItem
            {
                Id = $"kyc:{client.Name}",
                Title = "Refresh client KYC",
                Detail = $"{client.Name} needs compliance review before larger funding.",
                RelatedEntity = client.Name,
                OwnerRole = UserRole.Client,
                Category = ActionItemCategory.Compliance,
                Priority = client.KycStatus == KycStatus.RefreshRequired ? ActionItemPriority.High : ActionItemPriority.Medium,
                DueAt = DateTime.UtcNow.Date
            });
        }

        foreach (var debtor in portfolio.Debtors.Where(debtor => debtor.BuyDecision == BuyDecision.NoBuy || debtor.BuyDecision == BuyDecision.Watch))
        {
            items.Add(new ActionItem
            {
                Id = $"debtor:{debtor.Name}:{debtor.BuyDecision}",
                Title = debtor.BuyDecision == BuyDecision.NoBuy ? "Resolve no-buy debtor" : "Review watchlist debtor",
                Detail = $"{debtor.Name} is {debtor.BuyDecision.ToString().ToLowerInvariant()} with {debtor.AverageDaysToPay} average days to pay.",
                RelatedEntity = debtor.Name,
                OwnerRole = UserRole.Underwriter,
                Category = ActionItemCategory.Limit,
                Priority = debtor.BuyDecision == BuyDecision.NoBuy ? ActionItemPriority.Critical : ActionItemPriority.High,
                DueAt = DateTime.UtcNow.Date
            });
        }

        foreach (var invoice in portfolio.Invoices)
        {
            var decision = calculator.Decide(
                invoice,
                terms,
                portfolio.Clients.FirstOrDefault(client => client.Name == invoice.ClientName),
                portfolio.Debtors.FirstOrDefault(debtor => debtor.Name == invoice.Debtor),
                portfolio.Invoices);
            var requiredDocuments = invoice.Documents.Where(document => document.IsRequired).ToArray();
            var missingDocuments = requiredDocuments.Count(document => !document.IsSatisfied);
            if (missingDocuments > 0)
            {
                items.Add(new ActionItem
                {
                    Id = $"docs:{invoice.InvoiceNumber}",
                    Title = "Complete document pack",
                    Detail = $"{invoice.InvoiceNumber} is missing {missingDocuments} required document(s).",
                    RelatedEntity = invoice.InvoiceNumber,
                    OwnerRole = UserRole.Client,
                    Category = ActionItemCategory.Documents,
                    Priority = missingDocuments >= 3 ? ActionItemPriority.High : ActionItemPriority.Medium,
                    DueAt = DateTime.UtcNow.Date.AddDays(1)
                });
            }

            if (invoice.FundingStage == FundingStage.Submitted || invoice.FundingStage == FundingStage.Review)
            {
                items.Add(new ActionItem
                {
                    Id = $"uw:{invoice.InvoiceNumber}",
                    Title = decision.IsEligible ? "Approve eligible invoice" : "Clear underwriting blockers",
                    Detail = decision.IsEligible
                        ? $"{invoice.InvoiceNumber} has score {decision.Score} and is ready for decision."
                        : $"{invoice.InvoiceNumber}: {string.Join("; ", decision.Blockers.Take(2))}",
                    RelatedEntity = invoice.InvoiceNumber,
                    OwnerRole = UserRole.Underwriter,
                    Category = ActionItemCategory.Underwriting,
                    Priority = decision.IsEligible ? ActionItemPriority.Medium : ActionItemPriority.High,
                    DueAt = DateTime.UtcNow.Date
                });
            }
        }

        foreach (var collectionCase in portfolio.CollectionCases.Where(item => item.Status is CollectionStatus.Overdue or CollectionStatus.DueToday or CollectionStatus.PromiseToPay))
        {
            items.Add(new ActionItem
            {
                Id = $"collections:{collectionCase.InvoiceNumber}:{collectionCase.Status}",
                Title = collectionCase.Status == CollectionStatus.Overdue ? "Escalate overdue collection" : "Follow collection promise",
                Detail = $"{collectionCase.InvoiceNumber}: {collectionCase.NextAction}",
                RelatedEntity = collectionCase.InvoiceNumber,
                OwnerRole = UserRole.Operations,
                Category = ActionItemCategory.Collections,
                Priority = collectionCase.Status == CollectionStatus.Overdue ? ActionItemPriority.Critical : ActionItemPriority.High,
                DueAt = (collectionCase.PromiseToPayDate ?? collectionCase.DueDate).Date
            });
        }

        return items
            .Where(item => !completed.Contains(item.Id))
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.DueAt)
            .Take(12)
            .ToArray();
    }
}
