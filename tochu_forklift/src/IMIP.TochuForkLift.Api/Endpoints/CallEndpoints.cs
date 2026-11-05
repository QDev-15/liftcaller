using IMIP.TochuForkLift.Api.Services;
using IMIP.TochuForkLift.Shared.Contracts.Requests;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class CallEndpoints
{
    public static void MapCallEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/calls");

        group.MapPut("/{unitNo:int}", async (int unitNo, PutCallRequest request, ICallService service, CancellationToken ct) =>
        {
            var result = await service.PutCallAsync(unitNo, request.CallType, request.Palette, ct);
            return result switch
            {
                CallOperationResult.Success => Results.NoContent(),
                // Message body intentionally omitted — the client (WebApp) always shows its own
                // Loc-driven toast for this case (plan.md §6.4) and never reads this response's text.
                CallOperationResult.Conflict => Results.Conflict(),
                _ => Results.NotFound()
            };
        })
        .WithName("PutCall");

        group.MapDelete("/{unitNo:int}", async (int unitNo, ICallService service, CancellationToken ct) =>
        {
            var result = await service.CancelCallAsync(unitNo, ct);
            return result == CallOperationResult.Success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("CancelCall");

        group.MapPut("/{unitNo:int}/response", async (int unitNo, SetRespondingRequest request, ICallService service, CancellationToken ct) =>
        {
            var result = await service.SetRespondingAsync(unitNo, request.IsResponding, request.RespondedBy, ct);
            return result switch
            {
                CallOperationResult.Success => Results.NoContent(),
                // Not an error — the losing Receiver's WebApp treats 409 here as benign (plan.md §6.4)
                // and shows its own Loc-driven toast; this response's body is never read.
                CallOperationResult.Conflict => Results.Conflict(),
                _ => Results.NotFound()
            };
        })
        .WithName("SetResponding");

        group.MapPost("/{unitNo:int}/complete", async (int unitNo, CompleteCallRequest request, ICallService service, CancellationToken ct) =>
        {
            var result = await service.CompleteCallAsync(unitNo, request.CompletedBy, ct);
            return result == CallOperationResult.Success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("CompleteCall");
    }
}
