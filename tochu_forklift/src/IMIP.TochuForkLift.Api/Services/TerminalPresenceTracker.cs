namespace IMIP.TochuForkLift.Api.Services;

/// <summary>
/// A terminal can briefly hold more than one open SignalR connection (e.g. a tab reload racing
/// its own disconnect), so presence tracks connection IDs per terminal rather than a single bool —
/// the terminal only goes offline once its last connection drops.
/// </summary>
public class TerminalPresenceTracker : ITerminalPresenceTracker
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, HashSet<string>> _connections = [];
    private readonly Dictionary<int, DateTime> _lastSeen = [];

    public void Connected(int terminalId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(terminalId, out var set))
            {
                set = [];
                _connections[terminalId] = set;
            }
            set.Add(connectionId);
            _lastSeen[terminalId] = DateTime.UtcNow;
        }
    }

    public bool Disconnected(int terminalId, string connectionId)
    {
        lock (_lock)
        {
            _lastSeen[terminalId] = DateTime.UtcNow;
            if (!_connections.TryGetValue(terminalId, out var set)) return true;

            set.Remove(connectionId);
            if (set.Count != 0) return false;

            _connections.Remove(terminalId);
            return true;
        }
    }

    public bool IsOnline(int terminalId)
    {
        lock (_lock) { return _connections.TryGetValue(terminalId, out var set) && set.Count > 0; }
    }

    public DateTime? LastSeenAt(int terminalId)
    {
        lock (_lock) { return _lastSeen.TryGetValue(terminalId, out var t) ? t : null; }
    }
}
