using System;
using System.Collections.Generic;
using Loris.Models;
using Loris.Types;
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
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LorisDbContext).Assembly);
        modelBuilder.HasDefaultSchema("loris_schema");
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DevEui>().HaveConversion<DevEuiConverter>();
    }
}
