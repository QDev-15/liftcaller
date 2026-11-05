using System.Text.Json;
using IMIP.TochuForkLift.Shared.Client;
using Microsoft.JSInterop;

namespace IMIP.TochuForkLift.WebApp.Services;

/// <summary>
/// Persists the logged-in Terminal's identity in the browser's localStorage so a tablet only
/// needs to log in once — plan.md §4.3.
/// </summary>
public class AuthStateService(IJSRuntime js, ForkLiftApiClient api)
{
    private const string StorageKey = "forklift.terminalIdentity";

    public TerminalIdentity? Current { get; private set; }

    public async Task LoadAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        Current = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<TerminalIdentity>(json);
    }

    public async Task SetAsync(TerminalIdentity identity)
    {
        Current = identity;
        var json = JsonSerializer.Serialize(identity);
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task ClearAsync()
    {
        Current = null;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    /// <summary>
    /// Checks the cached identity against the server — if Admin deleted or deactivated this terminal,
    /// the API confirms it's gone (404) and this clears the cached identity so the caller can redirect
    /// to Login, instead of the tablet silently going on acting as a terminal that no longer exists.
    /// A network/server error is NOT treated as invalid — only a confirmed "no such active terminal"
    /// is, so a dropped Wi-Fi connection never logs the tablet out by itself.
    /// </summary>
    public async Task<bool> ValidateAsync(CancellationToken ct = default)
    {
        if (Current is null) return false;

        try
        {
            var terminal = await api.ValidateTerminalAsync(Current.TerminalId, ct);
            if (terminal is null)
            {
                await ClearAsync();
                return false;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }
}
