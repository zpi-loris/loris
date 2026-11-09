using System;
using System.Collections.Generic;
using Loris.Models;
using Microsoft.EntityFrameworkCore;

namespace Loris.Data;

public partial class LorisDbContext : DbContext
{
    public LorisDbContext() { }

    public LorisDbContext(DbContextOptions<LorisDbContext> options)
        : base(options) { }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<Function> Functions { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorReading> SensorReadings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseNpgsql(
            "CONFIGURE CONNECTION STRING" // TODO: CONFIGURE CONNECTION STRING
        );

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("arg_type", ["UNSPECIFIED", "VOID", "INT", "FLOAT", "STRING", "BOOL"])
            .HasPostgresEnum("device_status", ["PENDING", "ACTIVE", "BLOCKED"])
            .HasPostgresEnum("value_data_type", ["INT32", "FLOAT", "STRING", "BOOL"])
            .HasPostgresEnum(
                "value_type",
                [
                    "UNSPECIFIED",
                    "NUMERIC",
                    "TEXT",
                    "BOOL",
                    "TEMPERATURE",
                    "PRESSURE",
                    "FREQUENCY",
                    "MOISTURE",
                    "UPTIME",
                    "AREA",
                    "DISTANCE",
                    "DURATION",
                    "ILLUMINANCE",
                    "WATER",
                    "WEIGHT",
                    "AQI",
                    "CO2",
                    "PM1",
                    "PM25",
                    "PM4",
                    "PM10",
                    "HUMIDITY",
                    "WIND_SPEED",
                    "BATTERY",
                    "ENERGY",
                    "VOLTAGE",
                    "CURRENT",
                    "DATA_RATE",
                    "DATA_SIZE",
                    "SIGNAL_STRENGTH",
                    "PH",
                    "POTASSIUM",
                    "PHOSPHORUS",
                    "NITROGEN",
                ]
            );

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId).HasName("devices_pkey");

            entity.Property(e => e.DeviceId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Function>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("functions_pkey");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity
                .HasOne(d => d.Device)
                .WithMany(p => p.Functions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("functions_device_id_fkey");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sensors_pkey");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity
                .HasOne(d => d.Device)
                .WithMany(p => p.Sensors)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sensors_device_id_fkey");
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => new { e.SensorId, e.Time }).HasName("sensor_readings_pkey");

            entity
                .HasOne(d => d.Sensor)
                .WithMany(p => p.SensorReadings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sensor_readings_sensor_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DevEui>().HaveConversion<DevEuiConverter>();
    }
}
