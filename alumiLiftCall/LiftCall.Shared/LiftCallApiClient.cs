using System.Net;
using System.Net.Http.Json;

namespace LiftCall.Shared;

/// <summary>REST APIを呼び出すクライアント（呼出元・呼出先で共通）</summary>
public sealed class LiftCallApiClient : IDisposable
{
    private readonly HttpClient _http;

    public LiftCallApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    /// <summary>運転状態＋呼出一覧を取得（ポーリングで使用）</summary>
    public Task<StatusDto?> GetStatusAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<StatusDto>("api/status", ct);

    /// <summary>運転中／停車中を設定</summary>
    public async Task SetLiftStateAsync(bool isRunning, string? updatedBy = null, CancellationToken ct = default)
    {
        using var res = await _http.PutAsJsonAsync("api/lift-state",
            new SetLiftStateRequest(isRunning, updatedBy), ct);
        res.EnsureSuccessStatusCode();
    }

    /// <summary>呼出を登録（同じ号機の既存呼出は置き換え）</summary>
    public async Task PutCallAsync(int unitNo, CallType type, PaletteType palette, CancellationToken ct = default)
    {
        using var res = await _http.PutAsJsonAsync($"api/calls/{unitNo}",
            new PutCallRequest(type, palette), ct);
        res.EnsureSuccessStatusCode();
    }

    /// <summary>呼出をキャンセル</summary>
    public async Task CancelCallAsync(int unitNo, CancellationToken ct = default)
    {
        using var res = await _http.DeleteAsync($"api/calls/{unitNo}", ct);
        res.EnsureSuccessStatusCode();
    }

    /// <summary>応答中／対応待を設定</summary>
    public async Task SetRespondingAsync(int unitNo, bool isResponding, CancellationToken ct = default)
    {
        using var res = await _http.PutAsJsonAsync($"api/calls/{unitNo}/response",
            new SetRespondingRequest(isResponding), ct);
        // 他端末で完了・キャンセル済み（404）のときはエラーにしない
        if (res.StatusCode != HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }

    /// <summary>対応完了（履歴へ記録して呼出リストから削除）</summary>
    public async Task CompleteCallAsync(int unitNo, string? completedBy = null, CancellationToken ct = default)
    {
        using var res = await _http.PostAsJsonAsync($"api/calls/{unitNo}/complete",
            new CompleteCallRequest(completedBy), ct);
        // 他端末で完了・キャンセル済み（404）のときはエラーにしない
        if (res.StatusCode != HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}
