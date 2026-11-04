namespace IMIP.TochuForkLift.Shared.Contracts;

public class LiftStateDto
{
    public bool IsRunning { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
