using IMIP.TochuForkLift.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMIP.TochuForkLift.Api.Data.Configurations;

public class TerminalConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("Terminal", t =>
        {
            t.HasCheckConstraint("CK_Terminal_Role", "[Role] IN (1, 2)");
            t.HasCheckConstraint("CK_Terminal_UnitNo_RequiredForCaller", "[Role] = 2 OR [UnitNo] IS NOT NULL");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.PinHash).HasMaxLength(200).IsRequired();

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSDATETIME()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasPrecision(3);

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_Terminal_Name");

        // A UnitNo can only be claimed by one active Caller at a time (filtered unique index).
        builder.HasIndex(x => x.UnitNo)
            .IsUnique()
            .HasDatabaseName("UQ_Terminal_UnitNo_Active")
            .HasFilter("[Role] = 1 AND [IsActive] = 1");
    }
}
