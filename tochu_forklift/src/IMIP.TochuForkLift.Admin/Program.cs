using System.Security.Claims;
using IMIP.TochuForkLift.Admin.Components;
using IMIP.TochuForkLift.Admin.Services;
using IMIP.TochuForkLift.Shared.Client;
using IMIP.TochuForkLift.Shared.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection(AdminAuthOptions.SectionName));
builder.Services.AddSingleton<AdminAuthService>();

// Reused as-is from Shared — same REST client, same SignalR hub connection logic used by WebApp
// (plan.md §4.4: Admin talks to Api server-to-server, no CORS involved on this side).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7016/";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ForkLiftApiClient>();
builder.Services.AddScoped<AdminStateSyncService>();

// Language: a cookie (not localStorage) because Blazor Server needs it at render time, before any
// JS can run — same reasoning as cookie-based auth here. Read once per circuit from the request
// that started it; a plain GET below sets/updates the cookie and reloads.
builder.Services.AddHttpContextAccessor();
var defaultLanguage = AppLanguageExtensions.ParseCode(builder.Configuration["Localization:DefaultLanguage"]);
builder.Services.AddScoped(sp =>
{
    var http = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var cookie = http?.Request.Cookies["admin.lang"];
    return new Localizer(cookie is null ? defaultLanguage : AppLanguageExtensions.ParseCode(cookie));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Plain (non-Blazor) endpoints for sign-in/out — a real HTML form POST, not a Blazor event, because
// cookie auth needs HttpContext.SignInAsync during the actual request, which an interactive Blazor
// Server circuit no longer has access to once the connection is established.
app.MapPost("/account/login", async (HttpContext http, AdminAuthService auth) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (!auth.Validate(username, password))
    {
        return Results.Redirect("/login?error=1");
    }

    var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, username)],
        CookieAuthenticationDefaults.AuthenticationScheme));

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect("/");
});

app.MapPost("/account/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// Plain GET + redirect (not a Blazor event) so the new language is set before the next render,
// matching the cookie-at-render-time constraint noted above.
app.MapGet("/account/language", (HttpContext http, string lang, string? returnUrl) =>
{
    http.Response.Cookies.Append("admin.lang", AppLanguageExtensions.ParseCode(lang).ToCode(), new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        HttpOnly = false,
        SameSite = SameSiteMode.Lax
    });
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
