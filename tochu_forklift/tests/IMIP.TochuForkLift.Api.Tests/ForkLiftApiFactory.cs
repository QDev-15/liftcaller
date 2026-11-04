using IMIP.TochuForkLift.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IMIP.TochuForkLift.Api.Tests;

/// <summary>
/// Swaps the real SQL Server DbContext for an isolated SQLite in-memory one per factory instance
/// (plan.md §4.5). SQLite (not EF Core InMemory) because ExecuteUpdateAsync/ExecuteDeleteAsync —
/// used for the conditional-claim race handling in CallService — only work on a relational provider.
/// An in-memory SQLite database only lives as long as its connection stays open, so one connection
/// is held open for the factory's whole lifetime instead of letting EF open/close per-DbContext.
/// </summary>
public class ForkLiftApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ForkLiftApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(ForkLiftDbContext)))
                .ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            services.AddDbContext<ForkLiftDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
