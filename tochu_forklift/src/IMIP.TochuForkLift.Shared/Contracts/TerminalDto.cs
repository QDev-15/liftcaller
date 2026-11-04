namespace IMIP.TochuForkLift.Shared.Contracts;

/// <summary>Terminal info returned to clients — never includes PinHash.</summary>
public class TerminalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TerminalRole Role { get; set; }
    public int? UnitNo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Lightweight entry used by the login autocomplete — GET /api/auth/suggest.</summary>
public class TerminalSuggestionDto
{
    public string Name { get; set; } = string.Empty;
    public TerminalRole Role { get; set; }
}
