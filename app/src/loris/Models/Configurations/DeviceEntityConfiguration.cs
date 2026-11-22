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
        // https://www.npgsql.org/efcore/modeling/generated-properties.html#guiduuid-generation
        // npgsql will generate a new uuid for the primary key when inserting a new row.
        // it will use GUID v7, from dotnet version 9.0 and onwards

        // Unique indexes
        builder.HasIndex(e => e.DevEui).IsUnique();
        builder.HasIndex(e => e.FriendlyName).IsUnique();
    }
}
