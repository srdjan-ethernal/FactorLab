#if ENABLE_EFCORE
using FactorLab.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace FactorLab.Web.Persistence;

public sealed class FactorLabDbContext : DbContext
{
    public FactorLabDbContext(DbContextOptions<FactorLabDbContext> options) : base(options)
    {
    }

    public DbSet<ClientProfile> Clients => Set<ClientProfile>();
    public DbSet<FactoringTerms> FactoringTerms => Set<FactoringTerms>();
    public DbSet<FacilityApplication> FacilityApplications => Set<FacilityApplication>();
    public DbSet<DebtorProfile> Debtors => Set<DebtorProfile>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();
    public DbSet<UnderwritingDecision> UnderwritingDecisions => Set<UnderwritingDecision>();
    public DbSet<CollectionCase> CollectionCases => Set<CollectionCase>();
    public DbSet<CollectionActivity> CollectionActivities => Set<CollectionActivity>();
    public DbSet<PaymentMatch> PaymentMatches => Set<PaymentMatch>();
    public DbSet<DisputeCase> DisputeCases => Set<DisputeCase>();
    public DbSet<DebtorConfirmationRequest> DebtorConfirmationRequests => Set<DebtorConfirmationRequest>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<FundingBatch> FundingBatches => Set<FundingBatch>();
    public DbSet<ClientOffer> ClientOffers => Set<ClientOffer>();
    public DbSet<EvmTradeEvent> EvmTradeEvents => Set<EvmTradeEvent>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<IntegrationEvent> IntegrationEvents => Set<IntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientProfile>().HasKey("Id");
        modelBuilder.Entity<FactoringTerms>().HasKey("Id");
        modelBuilder.Entity<FacilityApplication>().HasKey(item => item.ApplicationNumber);
        modelBuilder.Entity<DebtorProfile>().HasKey("Id");
        modelBuilder.Entity<Invoice>().HasKey("Id");
        modelBuilder.Entity<DocumentRequirement>().HasKey("Id");
        modelBuilder.Entity<UnderwritingDecision>().HasKey("Id");
        modelBuilder.Entity<CollectionCase>().HasKey("Id");
        modelBuilder.Entity<CollectionActivity>().HasKey("Id");
        modelBuilder.Entity<PaymentMatch>().HasKey("Id");
        modelBuilder.Entity<DisputeCase>().HasKey("Id");
        modelBuilder.Entity<DebtorConfirmationRequest>().HasKey("Id");
        modelBuilder.Entity<LedgerEntry>().HasKey("Id");
        modelBuilder.Entity<FundingBatch>().HasKey("Id");
        modelBuilder.Entity<ClientOffer>().HasKey(item => item.OfferNumber);
        modelBuilder.Entity<EvmTradeEvent>().HasKey(item => item.EventId);
        modelBuilder.Entity<AuditEvent>().HasKey("Id");
        modelBuilder.Entity<IntegrationEvent>().HasKey(item => item.Id);

        modelBuilder.Entity<Invoice>().HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
        modelBuilder.Entity<FactoringTerms>().Property(item => item.AdvanceRate).HasPrecision(9, 2);
        modelBuilder.Entity<FactoringTerms>().Property(item => item.DiscountRatePer30Days).HasPrecision(9, 2);
        modelBuilder.Entity<FactoringTerms>().Property(item => item.ServiceFeeRate).HasPrecision(9, 2);
        modelBuilder.Entity<FactoringTerms>().Property(item => item.MinimumFee).HasPrecision(18, 2);
        modelBuilder.Entity<FactoringTerms>().Property(item => item.BankApr).HasPrecision(9, 2);
        modelBuilder.Entity<FactoringTerms>().OwnsOne(item => item.RiskPolicy, policy =>
        {
            policy.Property(item => item.ConcentrationWarningPercent).HasPrecision(9, 2);
            policy.Property(item => item.FacilityWarningUtilizationPercent).HasPrecision(9, 2);
            policy.Property(item => item.CreditWarningUtilizationPercent).HasPrecision(9, 2);
            policy.Property(item => item.HighDilutionPercent).HasPrecision(9, 2);
        });
        modelBuilder.Entity<FacilityApplication>().Property(item => item.Status).HasConversion<string>();
        modelBuilder.Entity<FacilityApplication>().Property(item => item.RequestedLimit).HasPrecision(18, 2);
        modelBuilder.Entity<FacilityApplication>().Property(item => item.MonthlyTurnover).HasPrecision(18, 2);
        modelBuilder.Entity<FacilityApplication>().Property(item => item.AverageInvoiceSize).HasPrecision(18, 2);
        modelBuilder.Entity<FacilityApplication>().Property(item => item.ApprovedLimit).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(invoice => invoice.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(invoice => invoice.ConcentrationPercent).HasPrecision(9, 2);
        modelBuilder.Entity<ClientProfile>().Property(client => client.FacilityLimit).HasPrecision(18, 2);
        modelBuilder.Entity<ClientProfile>().Property(client => client.ConcentrationLimitPercent).HasPrecision(9, 2);
        modelBuilder.Entity<DebtorProfile>().Property(debtor => debtor.CreditLimit).HasPrecision(18, 2);
        modelBuilder.Entity<DebtorProfile>().Property(debtor => debtor.DilutionPercent).HasPrecision(9, 2);
        modelBuilder.Entity<CollectionCase>().Property(item => item.AmountPaid).HasPrecision(18, 2);
        modelBuilder.Entity<CollectionCase>().Property(item => item.ChargebackAmount).HasPrecision(18, 2);
        modelBuilder.Entity<CollectionCase>().Property(item => item.ReserveReleased).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentMatch>().Property(item => item.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentMatch>().Property(item => item.Status).HasConversion<string>();
        modelBuilder.Entity<DisputeCase>().Property(item => item.Status).HasConversion<string>();
        modelBuilder.Entity<DisputeCase>().Property(item => item.DisputedAmount).HasPrecision(18, 2);
        modelBuilder.Entity<DisputeCase>().Property(item => item.CreditNoteAmount).HasPrecision(18, 2);
        modelBuilder.Entity<DebtorConfirmationRequest>().Property(item => item.Status).HasConversion<string>();
        modelBuilder.Entity<LedgerEntry>().Property(item => item.Debit).HasPrecision(18, 2);
        modelBuilder.Entity<LedgerEntry>().Property(item => item.Credit).HasPrecision(18, 2);
        modelBuilder.Entity<LedgerEntry>().Property(item => item.EntryType).HasConversion<string>();
        modelBuilder.Entity<FundingBatch>().Property(item => item.GrossReceivables).HasPrecision(18, 2);
        modelBuilder.Entity<FundingBatch>().Property(item => item.AdvanceAmount).HasPrecision(18, 2);
        modelBuilder.Entity<FundingBatch>().Property(item => item.EstimatedFees).HasPrecision(18, 2);
        modelBuilder.Entity<FundingBatch>().Property(item => item.ReserveHeld).HasPrecision(18, 2);
        modelBuilder.Entity<FundingBatch>().Property(item => item.NetCash).HasPrecision(18, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.Status).HasConversion<string>();
        modelBuilder.Entity<ClientOffer>().Property(item => item.GrossReceivables).HasPrecision(18, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.AdvanceAmount).HasPrecision(18, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.Fees).HasPrecision(18, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.ReserveHeld).HasPrecision(18, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.NetCash).HasPrecision(18, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.WeightedDays).HasPrecision(9, 2);
        modelBuilder.Entity<ClientOffer>().Property(item => item.EffectiveApr).HasPrecision(9, 2);
        modelBuilder.Entity<EvmTradeEvent>().Property(item => item.Action).HasConversion<string>();
        modelBuilder.Entity<EvmTradeEvent>().Property(item => item.Status).HasConversion<string>();
        modelBuilder.Entity<EvmTradeEvent>().Property(item => item.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<IntegrationEvent>().Property(item => item.Status).HasConversion<string>();

        modelBuilder.Entity<Invoice>().Ignore(invoice => invoice.Documents);
        modelBuilder.Entity<DocumentRequirement>().Ignore(document => document.IsSatisfied);

        base.OnModelCreating(modelBuilder);
    }
}
#endif
