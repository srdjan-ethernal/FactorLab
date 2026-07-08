using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IDisputeService
{
    DisputeCase Open(IPortfolioRepository portfolio, Invoice invoice, decimal disputedAmount, string reason, string owner);
    void Resolve(IPortfolioRepository portfolio, DisputeCase dispute, decimal creditNoteAmount, bool chargeback);
}
