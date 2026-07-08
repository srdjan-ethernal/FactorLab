using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public sealed class CurrentUserService
{
    public List<UserAccount> Users { get; } = new()
    {
        new UserAccount { DisplayName = "Srdjan Client", Email = "client@factorlab.local", Role = UserRole.Client, CompanyName = "Balkan Components d.o.o." },
        new UserAccount { DisplayName = "Mila Petrovic", Email = "mila@factorlab.local", Role = UserRole.Underwriter, CompanyName = "FactorLab" },
        new UserAccount { DisplayName = "Ana Ilic", Email = "ana@factorlab.local", Role = UserRole.Operations, CompanyName = "FactorLab" },
        new UserAccount { DisplayName = "Admin", Email = "admin@factorlab.local", Role = UserRole.Admin, CompanyName = "FactorLab" }
    };

    public UserAccount CurrentUser { get; private set; }

    public CurrentUserService()
    {
        CurrentUser = Users.First(user => user.Role == UserRole.Admin);
    }

    public void SwitchTo(string email)
    {
        var user = Users.FirstOrDefault(item => item.Email == email);
        if (user is not null)
        {
            CurrentUser = user;
        }
    }

    public bool CanSubmitFunding => CurrentUser.Role is UserRole.Client or UserRole.Admin;
    public bool CanUnderwrite => CurrentUser.Role is UserRole.Underwriter or UserRole.Admin;
    public bool CanOperateCollections => CurrentUser.Role is UserRole.Operations or UserRole.Admin;
    public bool CanAdminister => CurrentUser.Role == UserRole.Admin;
}
