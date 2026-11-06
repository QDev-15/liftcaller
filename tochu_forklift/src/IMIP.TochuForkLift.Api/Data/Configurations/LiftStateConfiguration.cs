using IMIP.TochuForkLift.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMIP.TochuForkLift.Api.Data.Configurations;

public class LiftStateConfiguration : IEntityTypeConfiguration<LiftState>
{
    public void Configure(EntityTypeBuilder<LiftState> builder)
    {
        builder.ToTable("LiftState");

        builder.HasKey(x => x.TerminalId);
        builder.Property(x => x.TerminalId).ValueGeneratedNever();

        builder.Property(x => x.IsRunning).IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasPrecision(3)
            .HasDefaultValueSql("SYSDATETIME()")
            .IsRequired();

        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        // One row per Receiver terminal — deleting the terminal drops its lift state with it.
        builder.HasOne<Terminal>()
            .WithOne()
            .HasForeignKey<LiftState>(x => x.TerminalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
