using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace IMIP.TochuForkLift.WebApp.Services;

/// <summary>
/// SignalR is the primary channel; a slow polling timer is just the safety net for a dropped
/// hub connection that hasn't noticed yet — plan.md §2, §6.
/// </summary>
public class StateSyncService(ForkLiftApiClient api) : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(12);
    private PeriodicTimer? _pollTimer;
    private Task? _pollLoop;
    private CancellationTokenSource? _cts;
    private int? _terminalId;

    public StateSnapshotDto Snapshot { get; private set; } = new();
    public HubConnectionState? ConnectionState => api.HubState;

    public event Action? Updated;
    public event Action? ConnectionStateChanged;

    /// <summary>terminalId identifies this tablet to the hub's presence tracking (Admin's online/offline
    /// column, and auto-stopping a lift whose Receiver tablet drops) — pass the logged-in terminal's id.</summary>
    public async Task InitializeAsync(int? terminalId = null)
    {
        _terminalId = terminalId;
        api.StateChanged += OnStateChanged;
        api.HubConnectionStateChanged += _ => ConnectionStateChanged?.Invoke();

        // Server may be unreachable right at startup (plan.md §8: "Server/IIS restart"). Swallow
        // failures here — the poll loop below keeps retrying both the snapshot and the hub connect.
        try
        {
            Snapshot = await api.GetStateAsync();
            Updated?.Invoke();
        }
        catch { /* retried by the poll loop */ }

        try { await api.StartHubAsync(terminalId: _terminalId); }
        catch { /* retried by the poll loop */ }

        _cts = new CancellationTokenSource();
        _pollTimer = new PeriodicTimer(PollInterval);
        _pollLoop = PollLoopAsync(_cts.Token);
    }

    private void OnStateChanged(StateSnapshotDto snapshot)
    {
        Snapshot = snapshot;
        Updated?.Invoke();
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _pollTimer!.WaitForNextTickAsync(ct))
            {
                try
                {
                    // Safety-net resync — picks up anything missed if the hub silently dropped.
                    var snapshot = await api.GetStateAsync(ct);
                    Snapshot = snapshot;
                    Updated?.Invoke();

                    // Also retries the initial hub connect if it never succeeded — StartHubAsync
                    // no-ops once already connected.
                    if (api.HubState is not Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                    {
                        await api.StartHubAsync(terminalId: _terminalId, ct: ct);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { /* server still unreachable — next tick retries */ }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on dispose.
        }
    }

    public async ValueTask DisposeAsync()
    {
        api.StateChanged -= OnStateChanged;
        _cts?.Cancel();
        _pollTimer?.Dispose();
        if (_pollLoop is not null)
        {
            try { await _pollLoop; } catch (OperationCanceledException) { }
        }
        await api.StopHubAsync();
    }
}
