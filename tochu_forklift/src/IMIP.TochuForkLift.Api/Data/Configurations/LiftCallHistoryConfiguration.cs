using IMIP.TochuForkLift.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMIP.TochuForkLift.Api.Data.Configurations;

/// <summary>
/// isSqlServer: the computed-column SQL below is T-SQL (DATEDIFF). It's only baked into the real
/// migration once (SQL Server is the only provider migrations are authored for — plan.md §5); this
/// flag just keeps Api.Tests' SQLite EnsureCreated() from trying to replay T-SQL it can't parse.
/// </summary>
public class LiftCallHistoryConfiguration(bool isSqlServer = true) : IEntityTypeConfiguration<LiftCallHistory>
{
    public void Configure(EntityTypeBuilder<LiftCallHistory> builder)
    {
        builder.ToTable("LiftCallHistory");

        builder.HasKey(x => x.HistoryId);
        builder.Property(x => x.HistoryId).ValueGeneratedOnAdd();

        builder.Property(x => x.CallType).IsRequired();
        builder.Property(x => x.Palette).IsRequired();
        builder.Property(x => x.CompletedBy).HasMaxLength(50);

        builder.Property(x => x.CalledAt).HasPrecision(3);
        builder.Property(x => x.RespondedAt).HasPrecision(3);
        builder.Property(x => x.CompletedAt).HasPrecision(3);

        if (isSqlServer)
        {
            builder.Property(x => x.DurationSeconds)
                .HasComputedColumnSql("DATEDIFF(SECOND, [CalledAt], [CompletedAt])", stored: true)
                .ValueGeneratedOnAddOrUpdate();

            builder.Property(x => x.ResponseWaitSeconds)
                .HasComputedColumnSql("DATEDIFF(SECOND, [CalledAt], [RespondedAt])", stored: true)
                .ValueGeneratedOnAddOrUpdate();
        }

        builder.HasIndex(x => x.CompletedAt).HasDatabaseName("IX_LiftCallHistory_CompletedAt");
        // Composite, matching alumiLiftCall's Sql/01_CreateTables.sql: IX_LiftCallHistory_UnitNo (UnitNo, CompletedAt).
        builder.HasIndex(x => new { x.UnitNo, x.CompletedAt }).HasDatabaseName("IX_LiftCallHistory_UnitNo");
    }
}
