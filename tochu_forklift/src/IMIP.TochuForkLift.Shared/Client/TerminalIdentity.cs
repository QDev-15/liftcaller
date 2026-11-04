using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Shared.Client;

/// <summary>Cached in the browser's localStorage after a successful login — see plan.md §4.3.</summary>
public class TerminalIdentity
{
    public int TerminalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TerminalRole Role { get; set; }
    public int? UnitNo { get; set; }
}
