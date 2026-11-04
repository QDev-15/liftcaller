namespace IMIP.TochuForkLift.Shared.Contracts.Requests;

public class CreateTerminalRequest
{
    public string Name { get; set; } = string.Empty;
    public TerminalRole Role { get; set; }
    public int? UnitNo { get; set; }
    public string Pin { get; set; } = "0000";
}

public class UpdateTerminalRequest
{
    public string Name { get; set; } = string.Empty;
    public int? UnitNo { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Null/empty = keep the current PIN unchanged.</summary>
    public string? NewPin { get; set; }
}
