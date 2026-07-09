#if ENABLE_EFCORE
using FactorLab.Web.Domain;
using FactorLab.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FactorLab.Web.Persistence;

public sealed class SqlPortfolioRepository : IPortfolioRepository, IPortfolioPersistence
{
    private readonly FactorLabDbContext db;
    private readonly SamplePortfolioService sampleSeed;
    private readonly PortfolioPersistenceOptions options;

    public SqlPortfolioRepository(FactorLabDbContext db, SamplePortfolioService sampleSeed, IOptions<PortfolioPersistenceOptions> options)
    {
        this.db = db;
        this.sampleSeed = sampleSeed;
        this.options = options.Value;
        Reload();
    }

    public string ProviderName => "Azure SQL / EF Core";
    public bool IsDurable => true;
    public FactoringTerms Terms { get; private set; } = new();
    public List<ClientProfile> Clients { get; private set; } = new();
    public List<FacilityApplication> FacilityApplications { get; private set; } = new();
    public List<DebtorProfile> Debtors { get; private set; } = new();
    public List<Invoice> Invoices { get; private set; } = new();
    public List<UnderwritingDecision> UnderwritingDecisions { get; private set; } = new();
    public List<AuditEvent> AuditEvents { get; private set; } = new();
    public List<CollectionCase> CollectionCases { get; private set; } = new();
    public List<CollectionActivity> CollectionActivities { get; private set; } = new();
    public List<FundingBatch> FundingBatches { get; private set; } = new();
    public List<ClientOffer> ClientOffers { get; private set; } = new();
    public List<EvmTradeEvent> EvmTradeEvents { get; private set; } = new();
    public List<PaymentMatch> PaymentMatches { get; private set; } = new();
    public List<DisputeCase> DisputeCases { get; private set; } = new();
    public List<DebtorConfirmationRequest> DebtorConfirmations { get; private set; } = new();

    public void Reload()
    {
        Terms = db.FactoringTerms.FirstOrDefault() ?? new FactoringTerms();
        Clients = db.Clients.ToList();
        FacilityApplications = db.FacilityApplications.ToList();
        Debtors = db.Debtors.ToList();
        Invoices = db.Invoices.ToList();
        AttachDocuments(db.DocumentRequirements.ToList());
        UnderwritingDecisions = db.UnderwritingDecisions.ToList();
        AuditEvents = db.AuditEvents.OrderByDescending(item => item.OccurredAt).ToList();
        CollectionCases = db.CollectionCases.ToList();
        CollectionActivities = db.CollectionActivities.ToList();
        FundingBatches = db.FundingBatches.ToList();
        ClientOffers = db.ClientOffers.ToList();
        EvmTradeEvents = db.EvmTradeEvents.ToList();
        PaymentMatches = db.PaymentMatches.ToList();
        DisputeCases = db.DisputeCases.ToList();
        DebtorConfirmations = db.DebtorConfirmationRequests.ToList();

        if (options.UseSampleSeedWhenEmpty && Clients.Count == 0 && Invoices.Count == 0)
        {
            LoadSampleSeed();
        }
    }

    public int SaveChanges()
    {
        TrackNew(db.FactoringTerms, new[] { Terms }, _ => "active");
        TrackNew(db.Clients, Clients, item => item.Name);
        TrackNew(db.FacilityApplications, FacilityApplications, item => item.ApplicationNumber);
        TrackNew(db.Debtors, Debtors, item => item.Name);
        TrackNew(db.Invoices, Invoices, item => item.InvoiceNumber);
        TrackNew(db.DocumentRequirements, Invoices.SelectMany(item => item.Documents), item => $"{item.InvoiceNumber}|{item.Kind}");
        TrackNew(db.UnderwritingDecisions, UnderwritingDecisions, item => item.InvoiceNumber);
        TrackNew(db.AuditEvents, AuditEvents, item => $"{item.OccurredAt:O}|{item.Entity}|{item.Action}|{item.Detail}");
        TrackNew(db.CollectionCases, CollectionCases, item => item.InvoiceNumber);
        TrackNew(db.CollectionActivities, CollectionActivities, item => $"{item.OccurredAt:O}|{item.InvoiceNumber}|{item.ActivityType}|{item.Note}");
        TrackNew(db.FundingBatches, FundingBatches, item => item.BatchNumber);
        TrackNew(db.ClientOffers, ClientOffers, item => item.OfferNumber);
        TrackNew(db.EvmTradeEvents, EvmTradeEvents, item => item.EventId);
        TrackNew(db.PaymentMatches, PaymentMatches, item => item.Reference);
        TrackNew(db.DisputeCases, DisputeCases, item => item.CaseNumber);
        TrackNew(db.DebtorConfirmationRequests, DebtorConfirmations, item => item.RequestNumber);
        return db.SaveChanges();
    }

    public UnderwritingDecision DecisionFor(string invoiceNumber)
    {
        var decision = UnderwritingDecisions.FirstOrDefault(item => item.InvoiceNumber == invoiceNumber);
        if (decision is not null) return decision;

        decision = new UnderwritingDecision
        {
            InvoiceNumber = invoiceNumber,
            Status = UnderwritingDecisionStatus.Pending,
            AssignedTo = "Unassigned"
        };
        UnderwritingDecisions.Add(decision);
        return decision;
    }

    public CollectionCase CollectionCaseFor(string invoiceNumber)
    {
        var collectionCase = CollectionCases.FirstOrDefault(item => item.InvoiceNumber == invoiceNumber);
        if (collectionCase is not null) return collectionCase;

        collectionCase = new CollectionCase
        {
            InvoiceNumber = invoiceNumber,
            DueDate = DateTime.UtcNow.Date.AddDays(45),
            Status = CollectionStatus.NotDue,
            Owner = "Collections Team",
            NextAction = "Monitor until due date"
        };
        CollectionCases.Add(collectionCase);
        return collectionCase;
    }

    private void AttachDocuments(IReadOnlyCollection<DocumentRequirement> documents)
    {
        foreach (var invoice in Invoices)
        {
            invoice.Documents.Clear();
            foreach (var document in documents.Where(item => item.InvoiceNumber == invoice.InvoiceNumber))
            {
                invoice.Documents.Add(document);
            }
        }
    }

    private void LoadSampleSeed()
    {
        Terms = sampleSeed.Terms;
        Clients = sampleSeed.Clients.ToList();
        FacilityApplications = sampleSeed.FacilityApplications.ToList();
        Debtors = sampleSeed.Debtors.ToList();
        Invoices = sampleSeed.Invoices.ToList();
        UnderwritingDecisions = sampleSeed.UnderwritingDecisions.ToList();
        AuditEvents = sampleSeed.AuditEvents.ToList();
        CollectionCases = sampleSeed.CollectionCases.ToList();
        CollectionActivities = sampleSeed.CollectionActivities.ToList();
        FundingBatches = sampleSeed.FundingBatches.ToList();
        ClientOffers = sampleSeed.ClientOffers.ToList();
        EvmTradeEvents = sampleSeed.EvmTradeEvents.ToList();
        PaymentMatches = sampleSeed.PaymentMatches.ToList();
        DisputeCases = sampleSeed.DisputeCases.ToList();
        DebtorConfirmations = sampleSeed.DebtorConfirmations.ToList();
    }

    private static void TrackNew<T>(DbSet<T> set, IEnumerable<T> items, Func<T, string> keySelector)
        where T : class
    {
        var localKeys = set.Local.Select(keySelector).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!localKeys.Contains(keySelector(item)))
            {
                set.Add(item);
            }
        }
    }
}
#endif
