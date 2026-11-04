namespace IMIP.TochuForkLift.Api.Services;

public enum CallOperationResult
{
    Success,
    /// <summary>Lost a race / state no longer matches the precondition — maps to HTTP 409.</summary>
    Conflict,
    NotFound
}
