using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IDebtorConfirmationService
{
    DebtorConfirmationRequest Send(IPortfolioRepository portfolio, Invoice invoice, string sentBy);
    void Confirm(IPortfolioRepository portfolio, DebtorConfirmationRequest request, string note);
    void Dispute(IPortfolioRepository portfolio, DebtorConfirmationRequest request, string note, IDisputeService disputeService, string owner);
}
