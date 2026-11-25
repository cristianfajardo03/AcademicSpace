namespace AcademicSpace.Api.Shared.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "AcademicSpace";
    public string Audience { get; set; } = "AcademicSpace";
    public string Key { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 8;
}
