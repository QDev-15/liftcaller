namespace IMIP.TochuForkLift.Shared.Contracts.Requests;

public class SetLiftStateRequest
{
    public int TerminalId { get; set; }
    public bool IsRunning { get; set; }
    public string? UpdatedBy { get; set; }
}
