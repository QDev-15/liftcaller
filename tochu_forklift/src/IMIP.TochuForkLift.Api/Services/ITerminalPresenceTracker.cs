namespace IMIP.TochuForkLift.Api.Services;

/// <summary>Live online/offline state derived straight from open SignalR connections — no DB
/// column, so it resets correctly (everyone offline) whenever the Api restarts.</summary>
public interface ITerminalPresenceTracker
{
    void Connected(int terminalId, string connectionId);

    /// <returns>true if this was the terminal's last open connection (it is now fully offline).</returns>
    bool Disconnected(int terminalId, string connectionId);

    bool IsOnline(int terminalId);
    DateTime? LastSeenAt(int terminalId);
}
