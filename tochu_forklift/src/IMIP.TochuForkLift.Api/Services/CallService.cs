using IMIP.TochuForkLift.Api.Data;
using IMIP.TochuForkLift.Api.Data.Entities;
using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace IMIP.TochuForkLift.Api.Services;

public class CallService(
    ForkLiftDbContext db,
    IStateBroadcaster broadcaster,
    ISignalLightNotifier signalLight,
    ILogger<CallService> logger) : ICallService
{
    public async Task<CallOperationResult> PutCallAsync(int unitNo, CallType callType, PaletteType palette, CancellationToken ct = default)
    {
        var existing = await db.LiftCalls.SingleOrDefaultAsync(c => c.UnitNo == unitNo, ct);

        // Fix for alumiLiftCall bug 9.1: never silently overwrite a call that's already being responded to.
        if (existing is not null && existing.IsResponding)
        {
            logger.LogWarning("Rejected re-call for unit {UnitNo} — already being responded to", unitNo);
            return CallOperationResult.Conflict;
        }

        if (existing is null)
        {
            db.LiftCalls.Add(new LiftCall
            {
                UnitNo = unitNo,
                CallType = (byte)callType,
                Palette = (byte)palette,
                CalledAt = DateTime.UtcNow,
                IsResponding = false
            });
        }
        else
        {
            existing.CallType = (byte)callType;
            existing.Palette = (byte)palette;
            existing.CalledAt = DateTime.UtcNow;
            existing.IsResponding = false;
            existing.RespondedAt = null;
            existing.RespondedBy = null;
        }

        await db.SaveChangesAsync(ct);
        await NotifyChangeAsync(ct);
        return CallOperationResult.Success;
    }

    public async Task<CallOperationResult> CancelCallAsync(int unitNo, CancellationToken ct = default)
    {
        var affected = await db.LiftCalls.Where(c => c.UnitNo == unitNo).ExecuteDeleteAsync(ct);
        if (affected == 0) return CallOperationResult.NotFound;

        await NotifyChangeAsync(ct);
        return CallOperationResult.Success;
    }

    public async Task<CallOperationResult> SetRespondingAsync(int unitNo, bool isResponding, string? respondedBy, CancellationToken ct = default)
    {
        int affected;

        if (isResponding)
        {
            // Conditional UPDATE — only succeeds if nobody has claimed it yet. SQL Server guarantees
            // exactly one concurrent request can win this; the loser gets Conflict (409), not an error — plan.md §6.4.
            affected = await db.LiftCalls
                .Where(c => c.UnitNo == unitNo && !c.IsResponding)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsResponding, true)
                    .SetProperty(c => c.RespondedAt, DateTime.UtcNow)
                    .SetProperty(c => c.RespondedBy, respondedBy), ct);

            if (affected == 0)
            {
                var exists = await db.LiftCalls.AnyAsync(c => c.UnitNo == unitNo, ct);
                return exists ? CallOperationResult.Conflict : CallOperationResult.NotFound;
            }
        }
        else
        {
            // Release the claim — any Receiver releasing is fine, no ownership check (plan.md §6.4: UI-level
            // filtering already means only the owning Receiver ever sees the "release" control).
            affected = await db.LiftCalls
                .Where(c => c.UnitNo == unitNo)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsResponding, false)
                    .SetProperty(c => c.RespondedAt, (DateTime?)null)
                    .SetProperty(c => c.RespondedBy, (string?)null), ct);

            if (affected == 0) return CallOperationResult.NotFound;
        }

        await NotifyChangeAsync(ct);
        return CallOperationResult.Success;
    }

    public async Task<CallOperationResult> CompleteCallAsync(int unitNo, string? completedBy, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var call = await db.LiftCalls.SingleOrDefaultAsync(c => c.UnitNo == unitNo, ct);
        if (call is null) return CallOperationResult.NotFound;

        var completedAt = DateTime.UtcNow;
        db.LiftCallHistories.Add(new LiftCallHistory
        {
            UnitNo = call.UnitNo,
            CallType = call.CallType,
            Palette = call.Palette,
            CalledAt = call.CalledAt,
            RespondedAt = call.RespondedAt,
            CompletedAt = completedAt,
            CompletedBy = completedBy
        });
        db.LiftCalls.Remove(call);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await NotifyChangeAsync(ct);
        return CallOperationResult.Success;
    }

    public async Task<List<HistoryEntryDto>> GetHistoryAsync(DateTime? from, DateTime? to, int? unitNo, CallType? callType, CancellationToken ct = default)
    {
        var query = db.LiftCallHistories.AsNoTracking().AsQueryable();

        if (from is not null) query = query.Where(h => h.CompletedAt >= from.Value);
        if (to is not null) query = query.Where(h => h.CompletedAt <= to.Value);
        if (unitNo is not null) query = query.Where(h => h.UnitNo == unitNo.Value);
        if (callType is not null) query = query.Where(h => h.CallType == (byte)callType.Value);

        return await query
            .OrderByDescending(h => h.CompletedAt)
            .Select(h => new HistoryEntryDto
            {
                HistoryId = h.HistoryId,
                UnitNo = h.UnitNo,
                CallType = (CallType)h.CallType,
                Palette = (PaletteType)h.Palette,
                CalledAt = h.CalledAt,
                RespondedAt = h.RespondedAt,
                CompletedAt = h.CompletedAt,
                CompletedBy = h.CompletedBy,
                DurationSeconds = h.DurationSeconds,
                ResponseWaitSeconds = h.ResponseWaitSeconds
            })
            .ToListAsync(ct);
    }

    /// <summary>
    /// Resolves the LED pattern and broadcasts the fresh snapshot after every write. The two DB reads
    /// (this method's own ResolveLedPatternAsync and StateBroadcaster.BuildSnapshotAsync) share this
    /// request's single scoped DbContext, so they must run one after another — EF Core's DbContext
    /// isn't safe for concurrent use. Only the LED's HTTP call (no DbContext involved) actually runs
    /// concurrently with the broadcast, matching plan.md §6.1 step 6's "neither depends on the other".
    /// </summary>
    private async Task NotifyChangeAsync(CancellationToken ct)
    {
        var pattern = await ResolveLedPatternAsync(ct);
        var ledTask = signalLight.NotifyAsync(pattern, CancellationToken.None);
        var broadcastTask = broadcaster.BroadcastAsync(ct);
        await Task.WhenAll(ledTask, broadcastTask);
    }

    private async Task<string> ResolveLedPatternAsync(CancellationToken ct)
    {
        var top = await db.LiftCalls
            .AsNoTracking()
            .OrderBy(c => c.CallType == (byte)CallType.Trouble ? 0 : 1)
            .ThenBy(c => c.CalledAt)
            .FirstOrDefaultAsync(ct);

        if (top is null) return "00000";
        // "11100" = Waiting (call exists, nobody responding yet); "22200" = Responding — matches
        // alumiLiftCall/LiftCall.Caller/LedController.cs's Waiting vs Responding patterns.
        if (top.IsResponding) return "22200";

        return (CallType)top.CallType switch
        {
            CallType.ProductPickup => "00100",
            CallType.ReturnMaterial => "01000",
            CallType.Trouble => "10000",
            _ => "00000"
        };
    }
}
