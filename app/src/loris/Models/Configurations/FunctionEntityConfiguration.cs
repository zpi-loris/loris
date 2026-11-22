using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

public class FunctionEntityConfiguration : IEntityTypeConfiguration<Function>
{
    public void Configure(EntityTypeBuilder<Function> builder)
    {
        // primary key
        builder.HasKey(e => e.FunctionId);
        // https://www.npgsql.org/efcore/modeling/generated-properties.html#guiduuid-generation
        // npgsql will generate a new uuid for the primary key when inserting a new row.
        // it will use GUID v7, from dotnet version 9.0 and onwards

        // NOTE:  EF core conventions opt in for VALUE GENERATION for Id ending properties
        // AND WE DON'T WANT THAT HERE
        builder.Property(e => e.DeviceFunctionId).ValueGeneratedNever();

        // relationship [p]rincipal - [d]ependant
        builder
            .HasOne(d => d.Device)
            .WithMany(p => p.Functions)
            .HasForeignKey(d => d.DeviceId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();
    }
}
