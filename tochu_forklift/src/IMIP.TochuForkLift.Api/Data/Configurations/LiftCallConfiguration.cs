using IMIP.TochuForkLift.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMIP.TochuForkLift.Api.Data.Configurations;

public class LiftCallConfiguration : IEntityTypeConfiguration<LiftCall>
{
    public void Configure(EntityTypeBuilder<LiftCall> builder)
    {
        builder.ToTable("LiftCall", t =>
        {
            t.HasCheckConstraint("CK_LiftCall_CallType", "[CallType] IN (1,2,3)");
            t.HasCheckConstraint("CK_LiftCall_Palette", "[Palette] BETWEEN 0 AND 3");
        });

        builder.HasKey(x => x.UnitNo);
        builder.Property(x => x.UnitNo).ValueGeneratedNever();

        builder.Property(x => x.CallType).IsRequired();
        builder.Property(x => x.Palette).IsRequired().HasDefaultValue((byte)0);

        builder.Property(x => x.CalledAt)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSDATETIME()")
            .IsRequired();

        builder.Property(x => x.RespondedAt).HasPrecision(3);

        builder.Property(x => x.IsResponding).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RespondedBy).HasMaxLength(50);

        // Composite, matching alumiLiftCall's Sql/01_CreateTables.sql: IX_LiftCall_CalledAt (CalledAt, UnitNo).
        builder.HasIndex(x => new { x.CalledAt, x.UnitNo }).HasDatabaseName("IX_LiftCall_CalledAt");
    }
}
