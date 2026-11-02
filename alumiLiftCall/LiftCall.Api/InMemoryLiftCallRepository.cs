using LiftCall.Shared;

namespace LiftCall.Api;

public class InMemoryLiftCallRepository : ILiftCallRepository
{
    private readonly object _lock = new();
    private readonly List<CallDto> _calls = new();
    private readonly List<(CallDto Call, DateTime CompletedAt)> _history = new(); // テスト用（再起動で消えます）
    private bool _isRunning = true;
    private DateTime _updatedAt = DateTime.Now;

    public Task<LiftStateDto> GetLiftStateAsync()
    {
        lock (_lock) return Task.FromResult(new LiftStateDto(_isRunning, _updatedAt));
    }

    public Task SetLiftStateAsync(bool isRunning, string? updatedBy)
    {
        lock (_lock)
        {
            _isRunning = isRunning;
            _updatedAt = DateTime.Now;
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CallDto>> GetCallsAsync()
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<CallDto>>(_calls.ToList());
    }

    public Task UpsertCallAsync(int unitNo, CallType type, PaletteType palette)
    {
        lock (_lock)
        {
            _calls.RemoveAll(c => c.UnitNo == unitNo);
            _calls.Add(new CallDto(unitNo, type, palette, DateTime.Now, false));
        }
        return Task.CompletedTask;
    }

    public Task DeleteCallAsync(int unitNo)
    {
        lock (_lock) _calls.RemoveAll(c => c.UnitNo == unitNo);
        return Task.CompletedTask;
    }

    public Task<bool> SetRespondingAsync(int unitNo, bool isResponding)
    {
        lock (_lock)
        {
            int index = _calls.FindIndex(c => c.UnitNo == unitNo);
            if (index < 0) return Task.FromResult(false);
            _calls[index] = _calls[index] with { IsResponding = isResponding };
            return Task.FromResult(true);
        }
    }

    public Task<bool> CompleteCallAsync(int unitNo, string? completedBy)
    {
        lock (_lock)
        {
            int index = _calls.FindIndex(c => c.UnitNo == unitNo);
            if (index < 0) return Task.FromResult(false);
            _history.Add((_calls[index], DateTime.Now));
            _calls.RemoveAt(index);
            return Task.FromResult(true);
        }
    }
}
