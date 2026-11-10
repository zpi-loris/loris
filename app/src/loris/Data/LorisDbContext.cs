using Loris.Models;
using Loris.Protocol;
using Loris.Types;
using Microsoft.EntityFrameworkCore;

namespace Loris.Data;

public partial class LorisDbContext : DbContext
{
    public LorisDbContext() { }

    public LorisDbContext(DbContextOptions<LorisDbContext> options)
        : base(options) { }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<Models.Function> Functions { get; set; }

    public virtual DbSet<Models.Sensor> Sensors { get; set; }

    public virtual DbSet<SensorReading> SensorReadings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .UseNpgsql(
                // "CONFIGURE CONNECTION STRING" // TODO: CONFIGURE CONNECTION STRING
                "Host=localhost;Port=5434;Database=loris;Username=postgres;Password=password",
                options =>
                    options
                        .MapEnum<DeviceStatus>()
                        .MapEnum<SensorValueType>()
                        .MapEnum<SensorSemantics>()
                        .MapEnum<FuncArgType>()
            )
            .UseSnakeCaseNamingConvention();

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
