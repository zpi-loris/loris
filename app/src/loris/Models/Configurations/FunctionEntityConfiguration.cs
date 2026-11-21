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
        builder
            .Property(e => e.FunctionId)
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

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
