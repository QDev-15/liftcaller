using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.Shared.Localization;
using IMIP.TochuForkLift.WebApp;
using IMIP.TochuForkLift.WebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var apiBaseUri = new Uri(apiBaseUrl, UriKind.RelativeOrAbsolute);
if (!apiBaseUri.IsAbsoluteUri)
{
    apiBaseUri = new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiBaseUri);
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseUri });
builder.Services.AddScoped<ForkLiftApiClient>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<StateSyncService>();
builder.Services.AddScoped<AlertSoundService>();

var defaultLanguage = AppLanguageExtensions.ParseCode(builder.Configuration["Localization:DefaultLanguage"]);
builder.Services.AddScoped(_ => new Localizer(defaultLanguage));

var host = builder.Build();

// Restore the saved language preference, if any, before anything renders — same reasoning as the
// Admin theme script: avoid a flash of the wrong language on every page load.
try
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    var saved = await js.InvokeAsync<string?>("localStorage.getItem", "forklift.language");
    if (!string.IsNullOrEmpty(saved))
    {
        host.Services.GetRequiredService<Localizer>().SetLanguage(AppLanguageExtensions.ParseCode(saved));
    }
}
catch { /* best-effort — falls back to the configured default */ }

await host.RunAsync();
