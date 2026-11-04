using IMIP.TochuForkLift.Shared.Contracts;
using Microsoft.JSInterop;

namespace IMIP.TochuForkLift.WebApp.Services;

/// <summary>
/// Only ever injected by Receiver.razor — operators drive the lift and can't watch the screen,
/// Callers don't need sound at all (plan.md §6.2). Plays request.wav / trouble.wav via JS interop
/// and repeats trouble.wav every 30s while any Trouble call is still unresponded.
/// </summary>
public class AlertSoundService : IAsyncDisposable
{
    private static readonly TimeSpan TroubleRepeatInterval = TimeSpan.FromSeconds(30);

    private readonly IJSRuntime _js;
    private readonly StateSyncService _sync;
    private readonly HashSet<(int UnitNo, DateTime CalledAt)> _seenCalling = [];
    private readonly PeriodicTimer _troubleRepeatTimer = new(TroubleRepeatInterval);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _troubleRepeatLoop;

    public AlertSoundService(IJSRuntime js, StateSyncService sync)
    {
        _js = js;
        _sync = sync;
        _sync.Updated += OnUpdated;
        _troubleRepeatLoop = TroubleRepeatLoopAsync(_cts.Token);
    }

    private void OnUpdated()
    {
        var calling = _sync.Snapshot.Calls.Where(c => !c.IsResponding).ToList();

        foreach (var c in calling)
        {
            var key = (c.UnitNo, c.CalledAt);
            // HashSet.Add returns true only the first time this exact (UnitNo, CalledAt) is seen.
            if (_seenCalling.Add(key))
            {
                _ = PlayAsync(c.CallType == CallType.Trouble ? "trouble" : "request");
            }
        }

        // Keep the "seen" set in sync with what's actually still calling (so a call that comes
        // back later — re-call after Cancel — is treated as new again).
        _seenCalling.RemoveWhere(k => !calling.Any(c => c.UnitNo == k.UnitNo && c.CalledAt == k.CalledAt));
    }

    private async Task TroubleRepeatLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _troubleRepeatTimer.WaitForNextTickAsync(ct))
            {
                var hasUnrespondedTrouble = _sync.Snapshot.Calls.Any(c => c.CallType == CallType.Trouble && !c.IsResponding);
                if (hasUnrespondedTrouble)
                {
                    await PlayAsync("trouble");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on dispose.
        }
    }

    private async Task PlayAsync(string name)
    {
        try { await _js.InvokeVoidAsync("forkliftSound.play", name); }
        catch { /* best-effort — a muted/blocked tab should never break the UI */ }
    }

    public async ValueTask DisposeAsync()
    {
        _sync.Updated -= OnUpdated;
        _cts.Cancel();
        _troubleRepeatTimer.Dispose();
        try { await _troubleRepeatLoop; } catch (OperationCanceledException) { }
    }
}
