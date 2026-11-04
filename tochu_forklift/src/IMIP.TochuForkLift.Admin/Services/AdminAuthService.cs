using Microsoft.Extensions.Options;

namespace IMIP.TochuForkLift.Admin.Services;

public class AdminAuthService(IOptions<AdminAuthOptions> options)
{
    public bool Validate(string username, string password)
    {
        var opt = options.Value;
        if (string.IsNullOrEmpty(opt.PasswordHash)) return false;
        if (!string.Equals(username, opt.Username, StringComparison.Ordinal)) return false;
        return BCrypt.Net.BCrypt.Verify(password, opt.PasswordHash);
    }
}
