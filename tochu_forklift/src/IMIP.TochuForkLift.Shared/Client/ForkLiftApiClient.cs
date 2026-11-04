using System.Net;
using System.Net.Http.Json;
using IMIP.TochuForkLift.Shared.Contracts;
using IMIP.TochuForkLift.Shared.Contracts.Requests;
using Microsoft.AspNetCore.SignalR.Client;

namespace IMIP.TochuForkLift.Shared.Client;

/// <summary>
/// Shared REST + SignalR client, reused by both WebApp (browser-to-server) and Admin
/// (server-to-server) — see plan.md §4.2 and §4.4. One instance per logical connection
/// (DI-scoped in both hosts).
/// </summary>
public class ForkLiftApiClient : IAsyncDisposable
{
    private readonly HttpClient _http;
    private HubConnection? _hub;

    public event Action<StateSnapshotDto>? StateChanged;
    public event Action<HubConnectionState>? HubConnectionStateChanged;

    public ForkLiftApiClient(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public HubConnectionState? HubState => _hub?.State;

    // ---- State ----

    public async Task<StateSnapshotDto> GetStateAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<StateSnapshotDto>("api/state", ct);
        return result ?? new StateSnapshotDto();
    }

    public async Task<List<HistoryEntryDto>> GetHistoryAsync(
        DateTime? from = null, DateTime? to = null, int? unitNo = null, CallType? callType = null, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (from is not null) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to is not null) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        if (unitNo is not null) query.Add($"unitNo={unitNo.Value}");
        if (callType is not null) query.Add($"callType={(int)callType.Value}");
        var url = "api/history" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var result = await _http.GetFromJsonAsync<List<HistoryEntryDto>>(url, ct);
        return result ?? [];
    }

    // ---- Lift state ----

    public async Task<ApiResult> SetLiftStateAsync(SetLiftStateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/lift-state", request, ct);
        return await ToResultAsync(response, ct);
    }

    // ---- Calls ----

    public async Task<ApiResult> PutCallAsync(int unitNo, PutCallRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/calls/{unitNo}", request, ct);
        return await ToResultAsync(response, ct);
    }

    public async Task<ApiResult> CancelCallAsync(int unitNo, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/calls/{unitNo}", ct);
        return await ToResultAsync(response, ct);
    }

    /// <summary>
    /// IsResponding = true can come back Conflict — that's a benign "someone else already
    /// took it" outcome, not an error. Caller shows a neutral toast and hides the card. See plan.md §6.4.
    /// </summary>
    public async Task<ApiResult> SetRespondingAsync(int unitNo, SetRespondingRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/calls/{unitNo}/response", request, ct);
        return await ToResultAsync(response, ct);
    }

    public async Task<ApiResult> CompleteCallAsync(int unitNo, CompleteCallRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/calls/{unitNo}/complete", request, ct);
        return await ToResultAsync(response, ct);
    }

    // ---- Auth ----

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
    }

    public async Task<List<TerminalSuggestionDto>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<TerminalSuggestionDto>>("api/auth/suggest", ct);
        return result ?? [];
    }

    // ---- Admin: Terminal management ----

    public async Task<List<TerminalDto>> GetTerminalsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<TerminalDto>>("api/admin/terminals", ct);
        return result ?? [];
    }

    public async Task<TerminalSaveResult> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/admin/terminals", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var terminal = await response.Content.ReadFromJsonAsync<TerminalDto>(ct);
            return new TerminalSaveResult(true, terminal);
        }
        return new TerminalSaveResult(false, null, await TryReadErrorMessageAsync(response, ct) ?? $"HTTP {(int)response.StatusCode}");
    }

    public async Task<TerminalSaveResult> UpdateTerminalAsync(int id, UpdateTerminalRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/terminals/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var terminal = await response.Content.ReadFromJsonAsync<TerminalDto>(ct);
            return new TerminalSaveResult(true, terminal);
        }
        return new TerminalSaveResult(false, null, await TryReadErrorMessageAsync(response, ct) ?? $"HTTP {(int)response.StatusCode}");
    }

    // ---- SignalR hub ----

    /// <summary>hubPath is relative to the HttpClient's BaseAddress, e.g. "hubs/state".</summary>
    public async Task StartHubAsync(string hubPath = "hubs/state", CancellationToken ct = default)
    {
        if (_hub is not null) return;

        var hubUrl = new Uri(_http.BaseAddress!, hubPath);
        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hub.On<StateSnapshotDto>("StateChanged", snapshot => StateChanged?.Invoke(snapshot));

        _hub.Reconnected += async _ =>
        {
            HubConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            // Resync in case anything changed while disconnected — see plan.md §6.
            var snapshot = await GetStateAsync();
            StateChanged?.Invoke(snapshot);
        };
        _hub.Reconnecting += _ =>
        {
            HubConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };
        _hub.Closed += _ =>
        {
            HubConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        try
        {
            await _hub.StartAsync(ct);
            HubConnectionStateChanged?.Invoke(HubConnectionState.Connected);
        }
        catch
        {
            // Initial connect failed (server down/unreachable) — HubConnection has no automatic
            // retry for this first attempt (WithAutomaticReconnect only covers drops *after* a
            // successful start). Tear down so a later StartHubAsync() call can try again, instead
            // of being silently no-op'd by the `_hub is not null` guard above.
            await _hub.DisposeAsync();
            _hub = null;
            HubConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            throw;
        }
    }

    /// <summary>
    /// Grabs and nulls <see cref="_hub"/> before doing anything else, so a concurrent second call
    /// (e.g. the owning page's own DisposeAsync racing the DI container's scope teardown calling
    /// this type's own DisposeAsync — both can happen for the same request in Blazor Server
    /// prerendering) sees it already null and returns immediately, instead of both racing to
    /// dispose the same HubConnection and one of them hitting ObjectDisposedException.
    /// </summary>
    public async Task StopHubAsync()
    {
        var hub = _hub;
        _hub = null;
        if (hub is null) return;

        try { await hub.StopAsync(); }
        catch (ObjectDisposedException) { /* already torn down by a concurrent caller */ }
        await hub.DisposeAsync();
    }

    private static async Task<ApiResult> ToResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK or HttpStatusCode.NoContent or HttpStatusCode.Created:
                return new ApiResult(ApiCallOutcome.Success);
            case HttpStatusCode.Conflict:
                return new ApiResult(ApiCallOutcome.Conflict);
            case HttpStatusCode.NotFound:
                return new ApiResult(ApiCallOutcome.NotFound);
            case HttpStatusCode.Unauthorized:
                return new ApiResult(ApiCallOutcome.Unauthorized);
            default:
                var message = await TryReadErrorMessageAsync(response, ct);
                return new ApiResult(ApiCallOutcome.Error, message ?? $"HTTP {(int)response.StatusCode}");
        }
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>(ct);
            return body?.Message;
        }
        catch
        {
            return null;
        }
    }

    private record ErrorBody(string? Message);

    public async ValueTask DisposeAsync() => await StopHubAsync();
}
