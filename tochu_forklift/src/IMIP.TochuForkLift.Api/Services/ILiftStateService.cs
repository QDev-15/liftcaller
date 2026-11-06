using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Services;

public interface ILiftStateService
{
    Task<List<LiftStateDto>> GetAllAsync(CancellationToken ct = default);
    Task SetAsync(int terminalId, bool isRunning, string? updatedBy, CancellationToken ct = default);
}
