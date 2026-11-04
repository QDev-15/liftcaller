using System.Text.Json.Serialization;

namespace IMIP.TochuForkLift.Shared.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaletteType : byte
{
    None = 0,
    A = 1,
    B = 2,
    C = 3
}
