using IMIP.TochuForkLift.Api.Data.Configurations;
using IMIP.TochuForkLift.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMIP.TochuForkLift.Api.Data;

public class ForkLiftDbContext(DbContextOptions<ForkLiftDbContext> options) : DbContext(options)
{
    public DbSet<LiftState> LiftStates => Set<LiftState>();
    public DbSet<LiftCall> LiftCalls => Set<LiftCall>();
    public DbSet<LiftCallHistory> LiftCallHistories => Set<LiftCallHistory>();
    public DbSet<Terminal> Terminals => Set<Terminal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isSqlServer = Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";

        modelBuilder.ApplyConfiguration(new LiftStateConfiguration());
        modelBuilder.ApplyConfiguration(new LiftCallConfiguration());
        modelBuilder.ApplyConfiguration(new LiftCallHistoryConfiguration(isSqlServer));
        modelBuilder.ApplyConfiguration(new TerminalConfiguration());
    }
}
