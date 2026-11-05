namespace IMIP.TochuForkLift.Shared.Client;

public enum ApiCallOutcome
{
    Success,
    /// <summary>Lost a race (e.g. another Receiver already claimed the call) — see plan.md §6.4. Not an error.</summary>
    Conflict,
    NotFound,
    Unauthorized,
    Error
}

public record ApiResult(ApiCallOutcome Outcome, string? Message = null)
{
    public bool IsSuccess => Outcome == ApiCallOutcome.Success;
}

/// <summary>ErrorCode is a Shared.Localization.Strings key (not a literal message) — resolve it via
/// the caller's own Localizer, e.g. Loc.Format(result.ErrorCode, result.ErrorArgs ?? []).</summary>
public record TerminalSaveResult(bool Success, Contracts.TerminalDto? Terminal, string? ErrorCode = null, object?[]? ErrorArgs = null);
