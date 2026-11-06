using IMIP.TochuForkLift.Api.Services;
using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request, ITerminalAuthService service, CancellationToken ct) =>
        {
            var (outcome, response) = await service.LoginAsync(request.Name, request.Pin, ct);
            return outcome switch
            {
                ApiCallOutcome.Success => Results.Ok(response),
                ApiCallOutcome.Conflict => Results.Conflict(),
                _ => Results.Unauthorized()
            };
        })
        .WithName("Login");

        group.MapGet("/suggest", async (ITerminalAuthService service, CancellationToken ct) =>
        {
            var suggestions = await service.GetSuggestionsAsync(ct);
            return Results.Ok(suggestions);
        })
        .WithName("GetLoginSuggestions");

        // Lets a cached client identity (WebApp localStorage) notice it's been deactivated/deleted
        // from Admin and log itself out, instead of sitting there silently calling as a ghost terminal.
        group.MapGet("/validate/{id:int}", async (int id, ITerminalAuthService service, CancellationToken ct) =>
        {
            var terminal = await service.ValidateAsync(id, ct);
            return terminal is not null ? Results.Ok(terminal) : Results.NotFound();
        })
        .WithName("ValidateTerminal");
    }
}
