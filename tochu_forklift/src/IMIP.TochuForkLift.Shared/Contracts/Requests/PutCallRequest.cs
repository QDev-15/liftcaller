namespace IMIP.TochuForkLift.Shared.Contracts.Requests;

public class PutCallRequest
{
    public CallType CallType { get; set; }
    public PaletteType Palette { get; set; } = PaletteType.None;
}
