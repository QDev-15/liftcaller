namespace IMIP.TochuForkLift.Shared.Contracts.Requests;

/// <summary>
/// IsResponding = true  → claim the call (RespondedBy must be set, conditional UPDATE WHERE IsResponding = 0 — see plan.md §6.4).
/// IsResponding = false → release the claim (RespondedBy reset to NULL server-side).
/// </summary>
public class SetRespondingRequest
{
    public bool IsResponding { get; set; }
    public string? RespondedBy { get; set; }
}
