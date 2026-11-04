namespace IMIP.TochuForkLift.Shared.Contracts;

public class HistoryEntryDto
{
    public long HistoryId { get; set; }
    public int UnitNo { get; set; }
    public CallType CallType { get; set; }
    public PaletteType Palette { get; set; }
    public DateTime CalledAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public int? DurationSeconds { get; set; }
    public int? ResponseWaitSeconds { get; set; }
}
