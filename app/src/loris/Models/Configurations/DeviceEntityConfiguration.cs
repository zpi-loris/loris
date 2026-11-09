using Loris.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

public class DeviceEntityConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        // primary key
        builder.HasKey(e => e.DeviceId);
        builder.Property(e => e.DeviceId).HasDefaultValue(Guid.CreateVersion7());

        // Unique indexes
        builder.HasIndex(e => e.DevEui).IsUnique();
        builder.HasIndex(e => e.FriendlyName).IsUnique();

        // text limits
        builder.Property(e => e.DeviceName).HasMaxLength(256);
        builder.Property(e => e.FriendlyName).HasMaxLength(256);

        // Value conversion for DevEui
        builder
            .Property(e => e.DevEui)
            .HasConversion(v => v.ToString(), v => DevEui.Parse(v))
            .HasMaxLength(16);

        // TODO: add LastSeen value auto updating
    }
}
