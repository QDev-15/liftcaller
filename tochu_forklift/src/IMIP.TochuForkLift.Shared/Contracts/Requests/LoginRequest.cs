namespace IMIP.TochuForkLift.Shared.Contracts.Requests;

public class LoginRequest
{
    public string Name { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int TerminalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TerminalRole Role { get; set; }
    public int? UnitNo { get; set; }
}
