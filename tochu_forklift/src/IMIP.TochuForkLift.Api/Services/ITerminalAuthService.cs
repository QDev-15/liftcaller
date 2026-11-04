using IMIP.TochuForkLift.Shared.Contracts;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Services;

public interface ITerminalAuthService
{
    Task<LoginResponse?> LoginAsync(string name, string pin, CancellationToken ct = default);
    Task<List<TerminalSuggestionDto>> GetSuggestionsAsync(CancellationToken ct = default);

    Task<List<TerminalDto>> GetTerminalsAsync(CancellationToken ct = default);
    Task<TerminalOperationResult> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken ct = default);
    Task<TerminalOperationResult> UpdateTerminalAsync(int id, UpdateTerminalRequest request, CancellationToken ct = default);
}
