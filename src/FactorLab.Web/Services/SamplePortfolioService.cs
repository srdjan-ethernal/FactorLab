using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public sealed class SamplePortfolioService : IPortfolioRepository
{
    public FactoringTerms Terms { get; } = new();

    public List<ClientProfile> Clients { get; } = new()
    {
        new ClientProfile { Name = "Balkan Components d.o.o.", Industry = "Wholesale components", Country = "Serbia", FacilityLimit = 150000m, ConcentrationLimitPercent = 40m, KycStatus = KycStatus.Approved, BuyDecision = BuyDecision.Buy, AccountManager = "Mila Petrovic" },
        new ClientProfile { Name = "GreenGrid Services Ltd", Industry = "Energy services", Country = "United Kingdom", FacilityLimit = 110000m, ConcentrationLimitPercent = 35m, KycStatus = KycStatus.Approved, BuyDecision = BuyDecision.Buy, AccountManager = "Luka Jovanovic" },
        new ClientProfile { Name = "Urban Supply Co.", Industry = "Food distribution", Country = "Serbia", FacilityLimit = 50000m, ConcentrationLimitPercent = 30m, KycStatus = KycStatus.RefreshRequired, BuyDecision = BuyDecision.Watch, AccountManager = "Ana Ilic" }
    };

    public List<DebtorProfile> Debtors { get; } = new()
    {
        new DebtorProfile { Name = "Adriatic Retail Group", Sector = "Retail", Country = "Croatia", CreditLimit = 70000m, Rating = DebtorRating.A, BuyDecision = BuyDecision.Buy, AverageDaysToPay = 37, DilutionPercent = 1.2m },
        new DebtorProfile { Name = "Northline Pharma", Sector = "Healthcare", Country = "Germany", CreditLimit = 85000m, Rating = DebtorRating.B, BuyDecision = BuyDecision.Buy, AverageDaysToPay = 54, DilutionPercent = 2.8m },
        new DebtorProfile { Name = "MetroBuild Supply", Sector = "Construction supply", Country = "Serbia", CreditLimit = 45000m, Rating = DebtorRating.C, BuyDecision = BuyDecision.Watch, AverageDaysToPay = 72, DilutionPercent = 4.5m },
        new DebtorProfile { Name = "GreenGrid Energy", Sector = "Energy", Country = "United Kingdom", CreditLimit = 95000m, Rating = DebtorRating.A, BuyDecision = BuyDecision.Buy, AverageDaysToPay = 43, DilutionPercent = 0.9m },
        new DebtorProfile { Name = "Luma Foods", Sector = "Food retail", Country = "Serbia", CreditLimit = 20000m, Rating = DebtorRating.D, BuyDecision = BuyDecision.NoBuy, AverageDaysToPay = 96, DilutionPercent = 9.4m }
    };

    public List<Invoice> Invoices { get; } = CreateInvoices();

    public List<UnderwritingDecision> UnderwritingDecisions { get; } = new()
    {
        new UnderwritingDecision { InvoiceNumber = "INV-2026-1042", Status = UnderwritingDecisionStatus.Approved, AssignedTo = "Mila Petrovic", DecisionNote = "Clean debtor and complete core documentation.", DecidedAt = DateTime.UtcNow.AddHours(-8) },
        new UnderwritingDecision { InvoiceNumber = "INV-2026-1051", Status = UnderwritingDecisionStatus.Pending, AssignedTo = "Mila Petrovic", DecisionNote = "Waiting for debtor confirmation.", Conditions = "Verify debtor confirmation before funding." },
        new UnderwritingDecision { InvoiceNumber = "INV-2026-1077", Status = UnderwritingDecisionStatus.ApprovedWithConditions, AssignedTo = "Luka Jovanovic", DecisionNote = "Proceed after PO is uploaded and confirmation is received.", Conditions = "PO/contract and debtor confirmation required.", DecidedAt = DateTime.UtcNow.AddHours(-3) },
        new UnderwritingDecision { InvoiceNumber = "INV-2026-1083", Status = UnderwritingDecisionStatus.Approved, AssignedTo = "Ana Ilic", DecisionNote = "Strong debtor, facility within limit.", DecidedAt = DateTime.UtcNow.AddDays(-1) },
        new UnderwritingDecision { InvoiceNumber = "INV-2026-1098", Status = UnderwritingDecisionStatus.Declined, AssignedTo = "Mila Petrovic", DecisionNote = "Dispute open and debtor marked no-buy.", DecidedAt = DateTime.UtcNow.AddHours(-2) }
    };

    public List<AuditEvent> AuditEvents { get; } = new()
    {
        new AuditEvent { Actor = "System", Entity = "INV-2026-1042", Action = "Scored", Detail = "Invoice passed eligibility checks." },
        new AuditEvent { Actor = "Mila Petrovic", Entity = "INV-2026-1042", Action = "Approved", Detail = "Approved for funding." },
        new AuditEvent { Actor = "Luka Jovanovic", Entity = "INV-2026-1077", Action = "Conditioned", Detail = "PO and debtor confirmation required." },
        new AuditEvent { Actor = "Mila Petrovic", Entity = "INV-2026-1098", Action = "Declined", Detail = "Disputed invoice and no-buy debtor." }
    };

    public List<CollectionCase> CollectionCases { get; } = new()
    {
        new CollectionCase { InvoiceNumber = "INV-2026-1042", DueDate = DateTime.UtcNow.Date.AddDays(6), Status = CollectionStatus.DueSoon, Owner = "Collections Team", NextAction = "Send payment reminder" },
        new CollectionCase { InvoiceNumber = "INV-2026-1051", DueDate = DateTime.UtcNow.Date.AddDays(-3), Status = CollectionStatus.PromiseToPay, PromiseToPayDate = DateTime.UtcNow.Date.AddDays(4), Owner = "Mila Petrovic", NextAction = "Confirm promise-to-pay by email" },
        new CollectionCase { InvoiceNumber = "INV-2026-1077", DueDate = DateTime.UtcNow.Date.AddDays(18), Status = CollectionStatus.NotDue, Owner = "Luka Jovanovic", NextAction = "Wait for debtor confirmation" },
        new CollectionCase { InvoiceNumber = "INV-2026-1083", DueDate = DateTime.UtcNow.Date.AddDays(-1), Status = CollectionStatus.Paid, AmountPaid = 87000m, ReserveReleased = 12635m, Owner = "Ana Ilic", NextAction = "Archive after reserve release" },
        new CollectionCase { InvoiceNumber = "INV-2026-1098", DueDate = DateTime.UtcNow.Date.AddDays(-12), Status = CollectionStatus.Chargeback, ChargebackAmount = 14200m, Owner = "Mila Petrovic", NextAction = "Chargeback to client after dispute review" }
    };

    public List<CollectionActivity> CollectionActivities { get; } = new()
    {
        new CollectionActivity { InvoiceNumber = "INV-2026-1042", ActivityType = CollectionActivityType.Email, ContactName = "ap@adriatic-retail.example", Note = "Reminder scheduled before due date.", OccurredAt = DateTime.UtcNow.AddDays(-1) },
        new CollectionActivity { InvoiceNumber = "INV-2026-1051", ActivityType = CollectionActivityType.PromiseToPay, ContactName = "Northline AP", Note = "Debtor promised payment next Friday.", OccurredAt = DateTime.UtcNow.AddHours(-6) },
        new CollectionActivity { InvoiceNumber = "INV-2026-1083", ActivityType = CollectionActivityType.PaymentReceived, ContactName = "GreenGrid AP", Note = "Full payment matched to invoice.", OccurredAt = DateTime.UtcNow.AddHours(-10) },
        new CollectionActivity { InvoiceNumber = "INV-2026-1083", ActivityType = CollectionActivityType.ReserveReleased, ContactName = "Operations", Note = "Reserve released after payment match.", OccurredAt = DateTime.UtcNow.AddHours(-8) },
        new CollectionActivity { InvoiceNumber = "INV-2026-1098", ActivityType = CollectionActivityType.Chargeback, ContactName = "Underwriting", Note = "Open dispute blocks collection.", OccurredAt = DateTime.UtcNow.AddHours(-2) }
    };

    public List<FundingBatch> FundingBatches { get; } = new();

    public List<ClientOffer> ClientOffers { get; } = new()
    {
        new ClientOffer
        {
            OfferNumber = "OFR-2026-0001",
            ClientName = "Balkan Components d.o.o.",
            Status = ClientOfferStatus.Sent,
            SentToEmail = "finance@balkan-components.example",
            InvoiceCount = 1,
            GrossReceivables = 42000m,
            AdvanceAmount = 35700m,
            Fees = 1205m,
            ReserveHeld = 6300m,
            NetCash = 34495m,
            WeightedDays = 34m,
            EffectiveApr = 36.2m,
            InvoiceNumbers = "INV-2026-1042",
            Notes = "Awaiting client acceptance in portal."
        }
    };

    public List<PaymentMatch> PaymentMatches { get; } = new()
    {
        new PaymentMatch { Reference = "BANK-20260706-001", InvoiceNumber = "INV-2026-1083", Debtor = "GreenGrid Energy", Amount = 87000m, Currency = "EUR", Status = PaymentMatchStatus.Matched, Note = "Sample payment already matched." }
    };

    public List<DisputeCase> DisputeCases { get; } = new()
    {
        new DisputeCase
        {
            CaseNumber = "DSP-2026-0001",
            InvoiceNumber = "INV-2026-1098",
            Status = DisputeStatus.Investigating,
            DisputedAmount = 14200m,
            Reason = "Quality dispute and debtor marked no-buy.",
            Owner = "Mila Petrovic",
            NextAction = "Collect client evidence and confirm chargeback path."
        }
    };

    public List<DebtorConfirmationRequest> DebtorConfirmations { get; } = new()
    {
        new DebtorConfirmationRequest
        {
            RequestNumber = "DCF-2026-0001",
            InvoiceNumber = "INV-2026-1042",
            Debtor = "Adriatic Retail Group",
            SentAt = DateTime.UtcNow.AddHours(-12),
            DueAt = DateTime.UtcNow.Date.AddDays(2),
            Status = DebtorConfirmationStatus.Confirmed,
            SentBy = "Mila Petrovic",
            ResponseNote = "Debtor confirmed invoice is valid and payable.",
            RespondedAt = DateTime.UtcNow.AddHours(-4)
        },
        new DebtorConfirmationRequest
        {
            RequestNumber = "DCF-2026-0002",
            InvoiceNumber = "INV-2026-1051",
            Debtor = "Northline Pharma",
            SentAt = DateTime.UtcNow.AddHours(-6),
            DueAt = DateTime.UtcNow.Date.AddDays(1),
            Status = DebtorConfirmationStatus.Sent,
            SentBy = "Mila Petrovic",
            ResponseNote = "Awaiting AP confirmation."
        }
    };

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

    private static List<Invoice> CreateInvoices()
    {
        var invoices = new List<Invoice>
        {
            new Invoice { ClientName = "Balkan Components d.o.o.", Debtor = "Adriatic Retail Group", InvoiceNumber = "INV-2026-1042", Amount = 42000m, DaysToPay = 34, Rating = DebtorRating.A, ConcentrationPercent = 22m, Status = InvoiceStatus.Clean, FundingStage = FundingStage.Approved },
            new Invoice { ClientName = "Balkan Components d.o.o.", Debtor = "Northline Pharma", InvoiceNumber = "INV-2026-1051", Amount = 63500m, DaysToPay = 58, Rating = DebtorRating.B, ConcentrationPercent = 31m, Status = InvoiceStatus.Clean, FundingStage = FundingStage.Review },
            new Invoice { ClientName = "Balkan Components d.o.o.", Debtor = "MetroBuild Supply", InvoiceNumber = "INV-2026-1077", Amount = 21800m, DaysToPay = 76, Rating = DebtorRating.C, ConcentrationPercent = 18m, Status = InvoiceStatus.Unverified, FundingStage = FundingStage.Submitted },
            new Invoice { ClientName = "GreenGrid Services Ltd", Debtor = "GreenGrid Energy", InvoiceNumber = "INV-2026-1083", Amount = 87000m, DaysToPay = 45, Rating = DebtorRating.A, ConcentrationPercent = 29m, Status = InvoiceStatus.Clean, FundingStage = FundingStage.Funded },
            new Invoice { ClientName = "Urban Supply Co.", Debtor = "Luma Foods", InvoiceNumber = "INV-2026-1098", Amount = 14200m, DaysToPay = 92, Rating = DebtorRating.D, ConcentrationPercent = 7m, Status = InvoiceStatus.Disputed, FundingStage = FundingStage.Draft }
        };

        SeedDocuments(invoices[0], DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Uploaded);
        SeedDocuments(invoices[1], DocumentStatus.Verified, DocumentStatus.Uploaded, DocumentStatus.Uploaded, DocumentStatus.Missing, DocumentStatus.Verified, DocumentStatus.Missing);
        SeedDocuments(invoices[2], DocumentStatus.Uploaded, DocumentStatus.Missing, DocumentStatus.Uploaded, DocumentStatus.Missing, DocumentStatus.Verified, DocumentStatus.Missing);
        SeedDocuments(invoices[3], DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified, DocumentStatus.Verified);
        SeedDocuments(invoices[4], DocumentStatus.Uploaded, DocumentStatus.Missing, DocumentStatus.Missing, DocumentStatus.Missing, DocumentStatus.Verified, DocumentStatus.Missing);

        return invoices;
    }

    private static void SeedDocuments(
        Invoice invoice,
        DocumentStatus invoiceStatus,
        DocumentStatus purchaseOrderStatus,
        DocumentStatus proofOfDeliveryStatus,
        DocumentStatus debtorConfirmationStatus,
        DocumentStatus kycStatus,
        DocumentStatus noticeStatus)
    {
        invoice.Documents.Add(DocumentChecklistFactory.Required(DocumentKind.Invoice, invoiceStatus));
        invoice.Documents.Add(DocumentChecklistFactory.Required(DocumentKind.PurchaseOrder, purchaseOrderStatus));
        invoice.Documents.Add(DocumentChecklistFactory.Required(DocumentKind.ProofOfDelivery, proofOfDeliveryStatus));
        invoice.Documents.Add(DocumentChecklistFactory.Required(DocumentKind.DebtorConfirmation, debtorConfirmationStatus));
        invoice.Documents.Add(DocumentChecklistFactory.Required(DocumentKind.Kyc, kycStatus));
        invoice.Documents.Add(DocumentChecklistFactory.Required(DocumentKind.NoticeOfAssignment, noticeStatus, noticeStatus == DocumentStatus.Missing ? "Generated after approval" : ""));
        foreach (var document in invoice.Documents)
        {
            document.InvoiceNumber = invoice.InvoiceNumber;
        }
    }
}
