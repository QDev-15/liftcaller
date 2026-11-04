using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Services;

/// <summary>
/// Single source of truth for composing the full state snapshot (always recomputed from the
/// DB, never cached — plan.md §7) and pushing it to every connected client via SignalR.
/// Shared by CallService, LiftStateService and GET /api/state so the sort/shape never drifts.
/// </summary>
public interface IStateBroadcaster
{
    Task<StateSnapshotDto> BuildSnapshotAsync(CancellationToken ct = default);
    Task BroadcastAsync(CancellationToken ct = default);
}
