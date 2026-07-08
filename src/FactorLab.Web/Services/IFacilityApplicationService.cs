using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IFacilityApplicationService
{
    FacilityApplication Submit(IPortfolioRepository portfolio, FacilityApplication application);
    void MoveToReview(FacilityApplication application, string assignedTo);
    void RequestMoreInfo(FacilityApplication application, string assignedTo);
    void Approve(IPortfolioRepository portfolio, FacilityApplication application, string approvedBy);
    void Decline(FacilityApplication application, string declinedBy);
}
