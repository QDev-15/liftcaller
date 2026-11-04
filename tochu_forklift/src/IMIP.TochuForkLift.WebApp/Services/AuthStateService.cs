using System.Text.Json;
using IMIP.TochuForkLift.Shared.Client;
using Microsoft.JSInterop;

namespace IMIP.TochuForkLift.WebApp.Services;

/// <summary>
/// Persists the logged-in Terminal's identity in the browser's localStorage so a tablet only
/// needs to log in once — plan.md §4.3.
/// </summary>
public class AuthStateService(IJSRuntime js)
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
}
