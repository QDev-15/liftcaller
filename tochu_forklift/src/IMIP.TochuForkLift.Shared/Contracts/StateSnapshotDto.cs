namespace IMIP.TochuForkLift.Shared.Contracts;

/// <summary>Full state snapshot, pushed over SignalR and returned by GET /api/state. Never diffed — always the complete picture.</summary>
public class StateSnapshotDto
{
    /// <summary>One entry per Receiver terminal — each terminal is its own lift. The lift is
    /// considered running overall when any entry here is running.</summary>
    public List<LiftStateDto> LiftStates { get; set; } = [];
    public List<CallDto> Calls { get; set; } = [];
}
