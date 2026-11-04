using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace IMIP.TochuForkLift.Admin.Services;

/// <summary>
/// Mirrors WebApp's StateSyncService. SignalR is the primary channel, but a periodic retry loop is
/// the safety net that keeps retrying GetState + StartHub even after the very first connect attempt
/// fails — HubConnection's own WithAutomaticReconnect() only covers a drop *after* a successful
/// start, so without this loop a Dashboard opened while Api is down never recovers once Api comes
/// back up (the stuck "Mất kết nối..." banner reported against the live site).
/// Scoped per Blazor Server circuit, so it stays alive and keeps retrying for as long as the
/// browser tab is open, independent of which Admin page is currently displayed.
/// </summary>
public class AdminStateSyncService(ForkLiftApiClient api) : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private PeriodicTimer? _pollTimer;
    private Task? _pollLoop;
    private CancellationTokenSource? _cts;
    private bool _initialized;

    public StateSnapshotDto Snapshot { get; private set; } = new();
    public bool IsConnected => api.HubState is HubConnectionState.Connected;

    public event Action? Updated;
    public event Action? ConnectionStateChanged;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        api.StateChanged += OnStateChanged;
        api.HubConnectionStateChanged += _ => ConnectionStateChanged?.Invoke();

        try
        {
            Snapshot = await api.GetStateAsync();
            Updated?.Invoke();
        }
        catch { /* retried by the poll loop below */ }

        try { await api.StartHubAsync(); }
        catch { /* retried by the poll loop below */ }

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
                    Snapshot = await api.GetStateAsync(ct);
                    Updated?.Invoke();

                    // Also retries the initial hub connect if it never succeeded — StartHubAsync
                    // no-ops once already connected.
                    if (api.HubState is not HubConnectionState.Connected)
                    {
                        await api.StartHubAsync(ct: ct);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { /* Api still unreachable — next tick retries */ }
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
