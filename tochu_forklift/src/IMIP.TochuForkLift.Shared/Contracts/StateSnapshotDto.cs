namespace IMIP.TochuForkLift.Shared.Contracts;

/// <summary>Full state snapshot, pushed over SignalR and returned by GET /api/state. Never diffed — always the complete picture.</summary>
public class StateSnapshotDto
{
    public LiftStateDto LiftState { get; set; } = new();
    public List<CallDto> Calls { get; set; } = [];
}
