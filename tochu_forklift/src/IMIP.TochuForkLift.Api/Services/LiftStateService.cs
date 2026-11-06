using IMIP.TochuForkLift.Api.Data;
using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace IMIP.TochuForkLift.Api.Services;

public class LiftStateService(ForkLiftDbContext db, IStateBroadcaster broadcaster) : ILiftStateService
{
    public async Task<List<LiftStateDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.LiftStates
            .AsNoTracking()
            .Select(x => new LiftStateDto
            {
                TerminalId = x.TerminalId,
                IsRunning = x.IsRunning,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToListAsync(ct);
    }

    public async Task SetAsync(int terminalId, bool isRunning, string? updatedBy, CancellationToken ct = default)
    {
        var state = await db.LiftStates.SingleOrDefaultAsync(x => x.TerminalId == terminalId, ct);
        if (state is null)
        {
            state = new Data.Entities.LiftState { TerminalId = terminalId };
            db.LiftStates.Add(state);
        }

        state.IsRunning = isRunning;
        state.UpdatedAt = DateTime.UtcNow;
        state.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        await broadcaster.BroadcastAsync(ct);
    }
}
