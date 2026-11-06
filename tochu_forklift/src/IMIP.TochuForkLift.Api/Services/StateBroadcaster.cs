using IMIP.TochuForkLift.Api.Data;
using IMIP.TochuForkLift.Api.Hubs;
using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IMIP.TochuForkLift.Api.Services;

public class StateBroadcaster(ForkLiftDbContext db, IHubContext<StateHub> hub, ITerminalPresenceTracker presence, ILogger<StateBroadcaster> logger) : IStateBroadcaster
{
    public async Task<StateSnapshotDto> BuildSnapshotAsync(CancellationToken ct = default)
    {
        var liftStates = (await db.LiftStates
            .AsNoTracking()
            .Select(x => new LiftStateDto
            {
                TerminalId = x.TerminalId,
                IsRunning = x.IsRunning,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToListAsync(ct))
            .Select(l => { l.IsOnline = presence.IsOnline(l.TerminalId); return l; })
            .ToList();

        // Trouble always first; within each tier, earliest call first — plan.md §6 (fixed, not configurable).
        var calls = await db.LiftCalls
            .AsNoTracking()
            .OrderBy(c => c.CallType == (byte)CallType.Trouble ? 0 : 1)
            .ThenBy(c => c.CalledAt)
            .Select(c => new CallDto
            {
                UnitNo = c.UnitNo,
                CallType = (CallType)c.CallType,
                Palette = (PaletteType)c.Palette,
                CalledAt = c.CalledAt,
                IsResponding = c.IsResponding,
                RespondedAt = c.RespondedAt,
                RespondedBy = c.RespondedBy
            })
            .ToListAsync(ct);

        return new StateSnapshotDto
        {
            LiftStates = liftStates,
            Calls = calls
        };
    }

    public async Task BroadcastAsync(CancellationToken ct = default)
    {
        var snapshot = await BuildSnapshotAsync(ct);
        await hub.Clients.All.SendAsync("StateChanged", snapshot, ct);
        logger.LogDebug("Broadcast StateChanged: {CallCount} active call(s), {RunningCount}/{LiftCount} lift(s) running",
            snapshot.Calls.Count, snapshot.LiftStates.Count(l => l.IsRunning), snapshot.LiftStates.Count);
    }
}
