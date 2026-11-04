using IMIP.TochuForkLift.Api.Services;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class StateEndpoints
{
    public static void MapStateEndpoints(this WebApplication app)
    {
        app.MapGet("/api/state", async (IStateBroadcaster broadcaster, CancellationToken ct) =>
        {
            var snapshot = await broadcaster.BuildSnapshotAsync(ct);
            return Results.Ok(snapshot);
        })
        .WithName("GetState");
    }
}
