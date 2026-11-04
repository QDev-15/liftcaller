using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Services;

public record TerminalOperationResult(bool Success, string? ErrorMessage = null, TerminalDto? Terminal = null)
{
    public static TerminalOperationResult Ok(TerminalDto terminal) => new(true, null, terminal);
    public static TerminalOperationResult Fail(string message) => new(false, message);
}
