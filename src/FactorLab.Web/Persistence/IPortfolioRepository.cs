using FactorLab.Web.Domain;

namespace FactorLab.Web.Persistence;

public interface IPortfolioRepository
{
    FactoringTerms Terms { get; }
    List<ClientProfile> Clients { get; }
    List<FacilityApplication> FacilityApplications { get; }
    List<DebtorProfile> Debtors { get; }
    List<Invoice> Invoices { get; }
    List<UnderwritingDecision> UnderwritingDecisions { get; }
    List<AuditEvent> AuditEvents { get; }
    List<CollectionCase> CollectionCases { get; }
    List<CollectionActivity> CollectionActivities { get; }
    List<FundingBatch> FundingBatches { get; }
    List<ClientOffer> ClientOffers { get; }
    List<EvmTradeEvent> EvmTradeEvents { get; }
    List<PaymentMatch> PaymentMatches { get; }
    List<DisputeCase> DisputeCases { get; }
    List<DebtorConfirmationRequest> DebtorConfirmations { get; }

    UnderwritingDecision DecisionFor(string invoiceNumber);
    CollectionCase CollectionCaseFor(string invoiceNumber);
}
