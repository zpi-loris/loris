using Loris.Data;
using Loris.Jobs;
using Loris.Models;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace loris.IntegrationTests.Jobs;

[Collection(TimescaleDbApplicationFactory.CollectionKey)]
public sealed class PreStartupStateCacheJobTests(TimescaleDbApplicationFactory appFactory)
    : IAsyncLifetime
{
    private readonly TimescaleDbApplicationFactory _appFactory = appFactory;

    public async ValueTask DisposeAsync()
    {
        await _appFactory.ResetDatabaseAsync();
        ((FakeStateCache)_appFactory.Services.GetRequiredService<IStateCache>()).Clear();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task StartAsync_PreStartupStateCacheJob_InitializesStateCache()
    {
        // SEED DATA
        Device device;
        using (var scope = _appFactory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();

            device = await SeedData(ctx);
        }

        // ARRANGE
        var scopeFactory = _appFactory.Services.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<PreStartupStateCacheJob>.Instance;
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();

        var job = new PreStartupStateCacheJob(logger, scopeFactory, stateCache);

        // ACT
        await job.StartAsync(CancellationToken.None);

        // ASSERT
        Assert.Equal(DevEui.Parse("1122AABBCCDDEEFF"), stateCache[device.DeviceId]);
        Assert.Equal(device.DeviceId, stateCache[DevEui.Parse("1122AABBCCDDEEFF")].DeviceId);
        Assert.Equal(
            DeviceStatus.ACTIVE,
            stateCache[DevEui.Parse("1122AABBCCDDEEFF")].DeviceStatus
        );

        Assert.True(
            stateCache.TryGetSensorLastReading(device.Sensors.First().SensorId, out var lastState)
        );
        Assert.NotNull(lastState);
        Assert.Equal(23.5f, lastState.FloatValue);
    }

    private static async Task<Device> SeedData(LorisDbContext ctx)
    {
        await ctx.Sensors.ExecuteDeleteAsync();
        await ctx.Functions.ExecuteDeleteAsync();
        await ctx.Devices.ExecuteDeleteAsync();

        var devEui = DevEui.Parse("1122AABBCCDDEEFF");

        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };

        var sensor = new Loris.Models.Sensor
        {
            DeviceSensorId = 1,
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
                Time = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0), // yesterday
                FloatValue = 23.5f,
            }
        );
        sensor.SensorReadings.Add(
            new SensorReading()
            {
                Time = DateTime.UtcNow - new TimeSpan(2, 0, 0, 0), // two days before
                FloatValue = 24.5f,
            }
        );

        // track entity and save
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync();

        return device;
    }
}
