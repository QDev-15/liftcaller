using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Services;

public interface ICallService
{
    Task<CallOperationResult> PutCallAsync(int unitNo, CallType callType, PaletteType palette, CancellationToken ct = default);
    Task<CallOperationResult> CancelCallAsync(int unitNo, CancellationToken ct = default);

    /// <summary>
    /// isResponding = true: conditional claim (fails with Conflict if already claimed — plan.md §6.4).
    /// isResponding = false: release the claim (RespondedBy reset to null).
    /// </summary>
    Task<CallOperationResult> SetRespondingAsync(int unitNo, bool isResponding, string? respondedBy, CancellationToken ct = default);

    Task<CallOperationResult> CompleteCallAsync(int unitNo, string? completedBy, CancellationToken ct = default);

    Task<List<HistoryEntryDto>> GetHistoryAsync(DateTime? from, DateTime? to, int? unitNo, CallType? callType, CancellationToken ct = default);
}
