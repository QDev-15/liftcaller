using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.Shared.Contracts;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Services;

public interface ITerminalAuthService
{
    /// <summary>Outcome is Success, Unauthorized (wrong name/PIN), or Conflict (correct credentials,
    /// but this terminal already has a live connection elsewhere — see StateHub's presence tracking).</summary>
    Task<(ApiCallOutcome Outcome, LoginResponse? Response)> LoginAsync(string name, string pin, CancellationToken ct = default);
    Task<List<TerminalSuggestionDto>> GetSuggestionsAsync(CancellationToken ct = default);

    /// <summary>Null if the terminal was deleted or deactivated since the caller last logged in — the
    /// client treats that as "log me out" (plan.md §6.4's "LAN-trust" model has no revocation list,
    /// so a cached identity is only ever invalidated by checking back with the server like this).</summary>
    Task<TerminalDto?> ValidateAsync(int terminalId, CancellationToken ct = default);

    Task<List<TerminalDto>> GetTerminalsAsync(CancellationToken ct = default);
    Task<TerminalOperationResult> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken ct = default);
    Task<TerminalOperationResult> UpdateTerminalAsync(int id, UpdateTerminalRequest request, CancellationToken ct = default);
}
