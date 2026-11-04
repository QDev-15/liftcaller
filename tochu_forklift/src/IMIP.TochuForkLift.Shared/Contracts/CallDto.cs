namespace IMIP.TochuForkLift.Shared.Contracts;

public class CallDto
{
    public int UnitNo { get; set; }
    public CallType CallType { get; set; }
    public PaletteType Palette { get; set; }
    public DateTime CalledAt { get; set; }
    public bool IsResponding { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; }
}
