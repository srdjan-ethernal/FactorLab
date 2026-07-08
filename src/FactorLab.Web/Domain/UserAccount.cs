namespace FactorLab.Web.Domain;

public sealed class UserAccount
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
    public string CompanyName { get; set; } = "";
}
