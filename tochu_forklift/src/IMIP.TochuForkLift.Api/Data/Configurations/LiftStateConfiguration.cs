using IMIP.TochuForkLift.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMIP.TochuForkLift.Api.Data.Configurations;

public class LiftStateConfiguration : IEntityTypeConfiguration<LiftState>
{
    public void Configure(EntityTypeBuilder<LiftState> builder)
    {
        builder.ToTable("LiftState", t => t.HasCheckConstraint("CK_LiftState_SingleRow", "[Id] = 1"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.IsRunning).IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSDATETIME()")
            .IsRequired();

        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // Fixed seed timestamp (not DateTime.UtcNow) so the migration snapshot is stable across regenerations.
        builder.HasData(new LiftState { Id = 1, IsRunning = false, UpdatedAt = new DateTime(2026, 1, 1), UpdatedBy = null });
    }
}
