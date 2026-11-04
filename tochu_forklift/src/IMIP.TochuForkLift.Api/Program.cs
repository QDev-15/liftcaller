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

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dev-only CORS: WebApp/Admin run on separate ports during `dotnet run`. In production they're
// reverse-proxied under the same IIS site as the Api, so no CORS is needed there (plan.md §8).
const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy => policy
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

// ---- Pipeline ----

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
}

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
