namespace IMIP.TochuForkLift.Api.Data.Entities;

/// <summary>Single-row table (Id always = 1) holding the lift's running/stopped state.</summary>
public class LiftState
{
    public int Id { get; set; }
    public bool IsRunning { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
