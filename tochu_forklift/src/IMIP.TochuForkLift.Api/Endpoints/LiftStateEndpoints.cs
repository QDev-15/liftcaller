using IMIP.TochuForkLift.Api.Services;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class LiftStateEndpoints
{
    public static void MapLiftStateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/lift-state");

        group.MapGet("/", async (ILiftStateService service, CancellationToken ct) =>
        {
            var states = await service.GetAllAsync(ct);
            return Results.Ok(states);
        })
        .WithName("GetLiftState");

        group.MapPut("/", async (SetLiftStateRequest request, ILiftStateService service, CancellationToken ct) =>
        {
            await service.SetAsync(request.TerminalId, request.IsRunning, request.UpdatedBy, ct);
            return Results.NoContent();
        })
        .WithName("SetLiftState");
    }
}
