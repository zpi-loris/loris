using Loris.Data;
using Loris.Jobs;
using Loris.Models;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace loris.IntegrationTests.Jobs;

public class PreStartupStateCacheJobTests(TimescaleDbApplicationFactory<Program> appFactory)
    : IClassFixture<TimescaleDbApplicationFactory<Program>>
{
    private readonly TimescaleDbApplicationFactory<Program> _appFactory = appFactory;

    [Fact]
    public async Task StartAsync_PreStartupStateCacheJob_InitializesStateCache()
    {
        // SEED DATA
        using (var scope = _appFactory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();

            await SeedData(ctx);
        }

        // ARRANGE
        var scopeFactory = _appFactory.Services.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<PreStartupStateCacheJob>.Instance;
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();

        var job = new PreStartupStateCacheJob(logger, scopeFactory, stateCache);

        // ACT
        await job.StartAsync(CancellationToken.None);

        // ASSERT
        Assert.True(stateCache.IsDeviceAccepted(DevEui.Parse("1122AABBCCDDEEFF")));
        Assert.True(
            stateCache.TryGetSensorLastState(DevEui.Parse("1122AABBCCDDEEFF"), 1, out var lastState)
        );
        Assert.NotNull(lastState);
        Assert.Equal(23.5f, lastState.FloatVal);
    }

    private static async Task SeedData(LorisDbContext ctx)
    {
        await ctx.Sensors.ExecuteDeleteAsync();
        await ctx.Functions.ExecuteDeleteAsync();
        await ctx.Devices.ExecuteDeleteAsync();

        var devEui = DevEui.Parse("1122AABBCCDDEEFF");

        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };

        var sensor = new Loris.Models.Sensor
        {
            DevEui = devEui,
            SensorId = 1,
            Name = "env_temp_sensor",
            Description = string.Empty,
            ValueType = SensorValueType.Float,
            Semantics = SensorSemantics.Temperature,
            Unit = "C",
        };

        device.Sensors.Add(sensor);

        sensor.SensorReadings.Add(
            new SensorReading()
            {
                DevEui = devEui,
                SensorId = 1,
                Time = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0), // yesterday
                FloatValue = 23.5f,
            }
        );
        sensor.SensorReadings.Add(
            new SensorReading()
            {
                DevEui = devEui,
                SensorId = 1,
                Time = DateTime.UtcNow - new TimeSpan(2, 0, 0, 0), // two days before
                FloatValue = 24.5f,
            }
        );

        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync();
    }
}
