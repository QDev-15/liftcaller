namespace IMIP.TochuForkLift.Admin.Services;

/// <summary>Bound from appsettings.json "AdminAuth" section — single shared account, fully separate
/// from the Terminal Name+PIN system used by Caller/Receiver (plan.md §4.4).</summary>
public class AdminAuthOptions
{
    public const string SectionName = "AdminAuth";

    public string Username { get; set; } = "admin";

    /// <summary>BCrypt hash — never store the plaintext password.</summary>
    public string PasswordHash { get; set; } = "";
}
