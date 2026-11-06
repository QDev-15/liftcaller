using IMIP.TochuForkLift.Api.Data;
using IMIP.TochuForkLift.Api.Endpoints;
using IMIP.TochuForkLift.Api.Hubs;
using IMIP.TochuForkLift.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ---- Services ----

builder.Services.AddDbContext<ForkLiftDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ImipLiftCall")));

builder.Services.AddSignalR();

builder.Services.AddScoped<IStateBroadcaster, StateBroadcaster>();
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<ILiftStateService, LiftStateService>();
builder.Services.AddScoped<ITerminalAuthService, TerminalAuthService>();

builder.Services.Configure<SignalLightOptions>(builder.Configuration.GetSection(SignalLightOptions.SectionName));
builder.Services.AddHttpClient(nameof(SignalLightNotifier));
builder.Services.AddSingleton<ISignalLightNotifier, SignalLightNotifier>();
builder.Services.AddSingleton<ITerminalPresenceTracker, TerminalPresenceTracker>();

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// WebApp runs as its own IIS site/port, so its browser calls to the Api (including the SignalR hub
// negotiate request) are cross-origin. Every device is on the same isolated factory LAN (plan.md
// §8 — never exposed to the internet), so any origin is allowed in every environment rather than
// maintaining a fixed port/IP allowlist. SetIsOriginAllowed (not AllowAnyOrigin) reflects the
// specific request's Origin back instead of a literal "*", which is required to combine with
// AllowCredentials() — the CORS spec forbids pairing a literal wildcard with credentials.
const string CorsPolicy = "Default";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ---- Create/migrate DB on startup — creates `imip_liftcall` and all tables on first run, no manual .sql needed (plan.md §5, §8) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForkLiftDbContext>();
    try
    {
        // Migrations are authored against SQL Server (the only production provider). Api.Tests swaps in
        // SQLite, which can't replay SqlServer-flavored migration SQL — it just builds the model directly.
        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        // A DB outage at boot shouldn't take the whole app down — /health and other non-DB
        // endpoints should stay reachable, and calls that actually touch the DB fail on their own
        // instead of the entire site returning IIS 500.30. Each later DB call retries independently.
        Log.Error(ex, "Database migration/connectivity check failed at startup — continuing without it");
    }
}

// ---- Pipeline ----

app.UseCors(CorsPolicy);

if (app.Configuration.GetValue("EnableSwagger", false) || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapStateEndpoints();
app.MapLiftStateEndpoints();
app.MapCallEndpoints();
app.MapHistoryEndpoints();
app.MapAuthEndpoints();
app.MapAdminTerminalEndpoints();

app.MapHub<StateHub>("/hubs/state");
app.MapHealthChecks("/health");

app.Run();

// Exposes the implicit top-level Program class so Api.Tests can use WebApplicationFactory<Program>.
public partial class Program;
