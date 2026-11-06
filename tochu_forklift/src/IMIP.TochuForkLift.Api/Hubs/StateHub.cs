using System.Collections.Concurrent;
using IMIP.TochuForkLift.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace IMIP.TochuForkLift.Api.Hubs;

/// <summary>
/// Clients only ever receive — pushed to from StateBroadcaster after every write. No client-to-hub
/// methods needed since all commands go through the REST endpoints (plan.md §6).
///
/// Callers and Receivers connect with a ?terminalId= query string (Admin's own observer connection
/// omits it, so it never shows up as a "terminal"). That lets presence be derived straight from the
/// SignalR connection itself (plan.md §5.4 "phát hiện offline") instead of a separate heartbeat
/// table: Admin's Terminals page shows live online/offline, and if a Receiver drops while its lift
/// is running, the lift is force-stopped so Callers never see a lift marked "running" with nobody
/// actually there to work it.
/// </summary>
public class StateHub(IServiceScopeFactory scopeFactory, ITerminalPresenceTracker presence, ILogger<StateHub> logger) : Hub
{
    // Wi-Fi on the factory floor drops for a second or two fairly often (plan.md §8) — WithAutomaticReconnect's
    // first two retries fire at 0s/2s, so this grace window rides those out without flapping the lift
    // state or the Admin terminal list every time a connection blips, while still reacting promptly
    // to an actual tablet power-off or closed browser tab.
    private static readonly TimeSpan OfflineGrace = TimeSpan.FromSeconds(8);
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> PendingOffline = new();

    public override async Task OnConnectedAsync()
    {
        if (GetTerminalId() is int terminalId)
        {
            if (PendingOffline.TryRemove(terminalId, out var pending)) await pending.CancelAsync();

            presence.Connected(terminalId, Context.ConnectionId);
            logger.LogInformation("Terminal {TerminalId} connected ({ConnectionId})", terminalId, Context.ConnectionId);

            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IStateBroadcaster>().BroadcastAsync();
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (GetTerminalId() is int terminalId && presence.Disconnected(terminalId, Context.ConnectionId))
        {
            logger.LogInformation("Terminal {TerminalId} disconnected ({ConnectionId}) — offline in {Grace}s unless it reconnects",
                terminalId, Context.ConnectionId, OfflineGrace.TotalSeconds);

            var cts = new CancellationTokenSource();
            PendingOffline[terminalId] = cts;
            _ = MarkOfflineAfterGraceAsync(terminalId, cts.Token);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task MarkOfflineAfterGraceAsync(int terminalId, CancellationToken ct)
    {
        try { await Task.Delay(OfflineGrace, ct); }
        catch (TaskCanceledException) { return; } // reconnected within the grace window

        PendingOffline.TryRemove(terminalId, out _);
        if (presence.IsOnline(terminalId)) return; // reconnected on a path that raced this check

        try
        {
            using var scope = scopeFactory.CreateScope();
            var liftState = scope.ServiceProvider.GetRequiredService<ILiftStateService>();

            var states = await liftState.GetAllAsync(ct);
            if (states.FirstOrDefault(s => s.TerminalId == terminalId) is { IsRunning: true })
            {
                logger.LogWarning("Terminal {TerminalId} went offline while its lift was running — stopping it", terminalId);
                await liftState.SetAsync(terminalId, false, "system (offline)", ct); // also broadcasts
            }
            else
            {
                await scope.ServiceProvider.GetRequiredService<IStateBroadcaster>().BroadcastAsync(ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to process terminal {TerminalId} going offline", terminalId);
        }
    }

    private int? GetTerminalId() =>
        int.TryParse(Context.GetHttpContext()?.Request.Query["terminalId"], out var id) ? id : null;
}
