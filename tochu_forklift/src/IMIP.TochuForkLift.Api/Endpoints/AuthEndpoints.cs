using IMIP.TochuForkLift.Api.Services;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request, ITerminalAuthService service, CancellationToken ct) =>
        {
            var result = await service.LoginAsync(request.Name, request.Pin, ct);
            return result is not null ? Results.Ok(result) : Results.Unauthorized();
        })
        .WithName("Login");

        group.MapGet("/suggest", async (ITerminalAuthService service, CancellationToken ct) =>
        {
            var suggestions = await service.GetSuggestionsAsync(ct);
            return Results.Ok(suggestions);
        })
        .WithName("GetLoginSuggestions");
    }
}
