using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Services;

public interface ILiftStateService
{
    Task<LiftStateDto> GetAsync(CancellationToken ct = default);
    Task SetAsync(bool isRunning, string? updatedBy, CancellationToken ct = default);
}
