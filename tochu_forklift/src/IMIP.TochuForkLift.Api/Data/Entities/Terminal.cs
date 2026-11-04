namespace IMIP.TochuForkLift.Api.Data.Entities;

/// <summary>New table (plan.md §5.1) — lets a Caller/Receiver tablet identify itself via Name + PIN.</summary>
public class Terminal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>1 = Caller, 2 = Receiver — see Shared.Contracts.TerminalRole.</summary>
    public byte Role { get; set; }

    /// <summary>Required when Role = Caller, NULL when Role = Receiver.</summary>
    public int? UnitNo { get; set; }

    /// <summary>BCrypt hash — the plaintext PIN is never stored.</summary>
    public string PinHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
