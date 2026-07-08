using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;

namespace FactorLab.Web.Services;

public interface IClientOfferService
{
    ClientOffer? CreateOffer(IPortfolioRepository portfolio, string clientName, string sentBy);
    void Send(ClientOffer offer);
    void Accept(IPortfolioRepository portfolio, ClientOffer offer, string acceptedBy);
    void Decline(ClientOffer offer, string declinedBy);
}
