namespace IMIP.TochuForkLift.Api.Data.Entities;

/// <summary>One active call per machine (UnitNo is the PK) — ported 1:1 from alumiLiftCall, plus RespondedBy (plan.md §5.2).</summary>
public class LiftCall
{
    public int UnitNo { get; set; }
    public byte CallType { get; set; }
    public byte Palette { get; set; }
    public DateTime CalledAt { get; set; }
    public bool IsResponding { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; }
}
