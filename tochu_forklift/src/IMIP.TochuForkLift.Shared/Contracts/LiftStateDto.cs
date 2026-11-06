namespace IMIP.TochuForkLift.Shared.Contracts;

public class LiftStateDto
{
    public int TerminalId { get; set; }
    public bool IsRunning { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Live, from the terminal's open SignalR connection. Callers treat a lift as actually
    /// "running" only when both this and IsRunning are true — a Receiver that drops stops counting
    /// instantly, instead of waiting out StateHub's offline grace period before IsRunning flips.</summary>
    public bool IsOnline { get; set; }
}
