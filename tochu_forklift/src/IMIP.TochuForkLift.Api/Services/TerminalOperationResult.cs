using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Services;

/// <summary>
/// ErrorCode is a Shared.Localization.Strings key, not a literal message — the Api has no notion of
/// the caller's chosen UI language, so the client (Admin) resolves the code via its own Localizer.
/// ErrorArgs fills any {0}/{1} placeholders in that string (e.g. the duplicate name, the unit number).
/// </summary>
public record TerminalOperationResult(bool Success, string? ErrorCode = null, object?[]? ErrorArgs = null, TerminalDto? Terminal = null)
{
    public static TerminalOperationResult Ok(TerminalDto terminal) => new(true, null, null, terminal);
    public static TerminalOperationResult Fail(string errorCode, params object?[] args) => new(false, errorCode, args);
}
