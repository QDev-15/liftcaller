namespace IMIP.TochuForkLift.Api.Data.Entities;

/// <summary>One row per Receiver terminal — each terminal is its own lift with its own
/// running/stopped state. Callers see the lift as Running when any row here is running.</summary>
public class LiftState
{
    public int TerminalId { get; set; }
    public bool IsRunning { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
