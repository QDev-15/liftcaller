using System.Text.Json.Serialization;

namespace IMIP.TochuForkLift.Shared.Contracts;

// Serialized as a string ("ProductPickup"), not the numeric value — plan.md §5: readable over the
// wire even though the DB column underneath is still a plain byte.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CallType : byte
{
    ProductPickup = 1,
    ReturnMaterial = 2,
    Trouble = 3
}
