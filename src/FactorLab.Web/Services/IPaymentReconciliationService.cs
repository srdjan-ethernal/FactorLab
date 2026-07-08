using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IPaymentReconciliationService
{
    PaymentReconciliationResult Reconcile(string csv, IPortfolioRepository portfolio);
}
