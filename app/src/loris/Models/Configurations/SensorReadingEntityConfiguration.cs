using CmdScale.EntityFrameworkCore.TimescaleDB.Configuration.Hypertable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loris.Models.Configurations;

public class SensorReadingEntityConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
    {
        // primary key
        builder.HasKey(e => new
        {
            e.DevEui,
            e.SensorId,
            e.Time,
        });

        // foreign keys
        builder
            .HasOne(d => d.Sensor)
            .WithMany(p => p.SensorReadings)
            .HasForeignKey(e => new { e.DevEui, e.SensorId })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // TimescaleDB hypertable
        builder
            .IsHypertable(e => e.Time)
            .WithChunkTimeInterval("1 day")
            .EnableCompression()
            .WithCompressionSegmentBy(e => e.SensorId)
            .WithCompressionOrderBy(OrderByBuilder.For<SensorReading>(e => e.Time).Descending())
            .WithCompressChunkTimeInterval("7 days");
    }
}
