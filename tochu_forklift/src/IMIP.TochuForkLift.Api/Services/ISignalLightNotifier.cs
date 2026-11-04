namespace IMIP.TochuForkLift.Api.Services;

/// <summary>
/// Sends the LED tower its on/off pattern. Called server-to-server only — never from the browser
/// (Blazor WASM can't reach the device: CORS + mixed-content, see plan.md §6.1).
/// </summary>
public interface ISignalLightNotifier
{
    /// <summary>Fire-and-forget: swallows every failure so a dead/unreachable light never blocks a call action.</summary>
    Task NotifyAsync(string pattern, CancellationToken ct = default);
}
