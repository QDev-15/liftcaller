using System.Text.Json.Serialization;

namespace IMIP.TochuForkLift.Shared.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TerminalRole : byte
{
    Caller = 1,
    Receiver = 2
}
