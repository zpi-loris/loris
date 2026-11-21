using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

public class DeviceEntityConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        // primary key
        builder.HasKey(e => e.DeviceId);
        builder
            .Property(e => e.DeviceId)
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

        // Unique indexes
        builder.HasIndex(e => e.DevEui).IsUnique();
        builder.HasIndex(e => e.FriendlyName).IsUnique();
    }
}
