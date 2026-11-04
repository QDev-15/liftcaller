using IMIP.TochuForkLift.Api.Data;
using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace IMIP.TochuForkLift.Api.Services;

public class LiftStateService(ForkLiftDbContext db, IStateBroadcaster broadcaster) : ILiftStateService
{
    public async Task<LiftStateDto> GetAsync(CancellationToken ct = default)
    {
        var state = await db.LiftStates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
        return new LiftStateDto
        {
            IsRunning = state?.IsRunning ?? false,
            UpdatedAt = state?.UpdatedAt ?? DateTime.UtcNow,
            UpdatedBy = state?.UpdatedBy
        };
    }

    public async Task SetAsync(bool isRunning, string? updatedBy, CancellationToken ct = default)
    {
        var state = await db.LiftStates.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (state is null)
        {
            state = new Data.Entities.LiftState { Id = 1 };
            db.LiftStates.Add(state);
        }

        state.IsRunning = isRunning;
        state.UpdatedAt = DateTime.UtcNow;
        state.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        await broadcaster.BroadcastAsync(ct);
    }
}
