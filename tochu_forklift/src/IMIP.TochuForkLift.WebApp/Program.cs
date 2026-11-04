using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.WebApp;
using IMIP.TochuForkLift.WebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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

await builder.Build().RunAsync();
