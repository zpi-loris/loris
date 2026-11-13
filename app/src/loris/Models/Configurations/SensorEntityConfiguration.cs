using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

public class SensorEntityConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        // primary key
        builder.HasKey(e => new { e.DevEui, e.SensorId });

        // NOTE:  EF core conventions opt in for VALUE GENERATION for Id ending properties
        // AND WE DON'T WANT THAT HERE
        builder.Property(e => e.SensorId).ValueGeneratedNever();

        // relationship [p]rincipal - [d]ependant
        builder
            .HasOne(d => d.Device)
            .WithMany(p => p.Sensors)
            .HasForeignKey(d => d.DevEui)
            .HasPrincipalKey(p => p.DevEui)
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();
    }
}
