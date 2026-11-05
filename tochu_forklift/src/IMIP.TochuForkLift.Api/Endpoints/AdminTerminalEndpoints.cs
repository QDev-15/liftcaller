using IMIP.TochuForkLift.Api.Services;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class AdminTerminalEndpoints
{
    // No extra auth gate here: Admin calls this server-to-server over the factory LAN, and access
    // to the Admin UI itself is already gated by AdminAuthService's cookie auth (plan.md §4.4).
    public static void MapAdminTerminalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/terminals");

        group.MapGet("/", async (ITerminalAuthService service, CancellationToken ct) =>
        {
            var terminals = await service.GetTerminalsAsync(ct);
            return Results.Ok(terminals);
        })
        .WithName("GetTerminals");

        group.MapPost("/", async (CreateTerminalRequest request, ITerminalAuthService service, CancellationToken ct) =>
        {
            var result = await service.CreateTerminalAsync(request, ct);
            return result.Success ? Results.Created($"/api/admin/terminals/{result.Terminal!.Id}", result.Terminal) : Results.BadRequest(new { code = result.ErrorCode, args = result.ErrorArgs });
        })
        .WithName("CreateTerminal");

        group.MapPut("/{id:int}", async (int id, UpdateTerminalRequest request, ITerminalAuthService service, CancellationToken ct) =>
        {
            var result = await service.UpdateTerminalAsync(id, request, ct);
            return result.Success ? Results.Ok(result.Terminal) : Results.BadRequest(new { code = result.ErrorCode, args = result.ErrorArgs });
        })
        .WithName("UpdateTerminal");
    }
}
