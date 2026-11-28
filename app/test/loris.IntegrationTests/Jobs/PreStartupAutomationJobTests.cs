using System.Diagnostics.CodeAnalysis;
using Loris.Automations;
using Loris.Data;
using Loris.Events;
using Loris.Jobs;
using Loris.Models;
using Loris.Protocol;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace loris.IntegrationTests.Jobs;

[Collection(TimescaleDbApplicationFactory.CollectionKey)]
public sealed class PreStartupAutomationJobTests(TimescaleDbApplicationFactory appFactory)
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
    public async Task StartAsync_PreStartupAutomationJob_InitializesAllValidAutomations()
    {
        // SEED
        Device device;
        using (var scope = _appFactory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
            device = await SeedDevice(ctx);
            await SeedValidAutomations(ctx);
        }

        // ARRANGE
        var factory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAutomationIndex>();
                services.AddSingleton<IAutomationIndex, FakedIndex>();
            });
        });
        var serviceFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var jsonOptions = factory.Services.GetRequiredService<IOptions<JsonOptions>>();
        var index = (FakedIndex)factory.Services.GetRequiredService<IAutomationIndex>();
        var logger = new NullLogger<PreStartupAutomationJob>();

        var job = new PreStartupAutomationJob(logger, serviceFactory, jsonOptions);

        // ACT
        await job.StartAsync(CancellationToken.None);

        // ASSERT
        //
        // there is one event key { DevEui, SensorId }
        Assert.Single(index._index);
        // there event key is of given structure
        Assert.True(
            index.TryGet(new SensorEvent(device.Sensors.First().SensorId), out var validAutomations)
        );
        // there are 2 automations that trigger on such event key
        Assert.Equal(2, validAutomations.Count);
    }

    [Fact]
    public async Task StartAsync_PreStartupAutomationJob_DiscardsInvalidAutomationsButLeaveValid()
    {
        // SEED
        Device device;
        using (var scope = _appFactory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
            device = await SeedDevice(ctx);
            await SeedAutomationsWithOneInvalid(ctx);
        }

        // ARRANGE
        var factory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAutomationIndex>();
                services.AddSingleton<IAutomationIndex, FakedIndex>();
            });
        });
        var serviceFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var jsonOptions = factory.Services.GetRequiredService<IOptions<JsonOptions>>();
        var index = (FakedIndex)factory.Services.GetRequiredService<IAutomationIndex>();
        var logger = new NullLogger<PreStartupAutomationJob>();

        var job = new PreStartupAutomationJob(logger, serviceFactory, jsonOptions);

        // ACT
        await job.StartAsync(CancellationToken.None);

        // ASSERT
        //
        // there is one event key { DevEui, SensorId }
        Assert.Single(index._index);
        // the event key is of given structure
        Assert.True(
            index.TryGet(new SensorEvent(device.Sensors.First().SensorId), out var validAutomations)
        );
        // there is 1 automation that trigger on such event key
        Assert.Single(validAutomations);
    }

    private static async Task<Device> SeedDevice(LorisDbContext ctx)
    {
        await ctx.Sensors.ExecuteDeleteAsync();
        await ctx.Functions.ExecuteDeleteAsync();
        await ctx.Devices.ExecuteDeleteAsync();

        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };

        device.Sensors.Add(
            new Loris.Models.Sensor
            {
                DeviceSensorId = 1,
                Name = "env_temp_sensor",
                Description = string.Empty,
                ValueType = SensorValueType.Float,
                Semantics = SensorSemantics.Temperature,
                Unit = "C",
            }
        );

        device.Sensors.Add(
            new Loris.Models.Sensor
            {
                DeviceSensorId = 2,
                Name = "moisture_sensor",
                Description = string.Empty,
                ValueType = SensorValueType.Float,
                Semantics = SensorSemantics.Moisture,
                Unit = "%",
            }
        );

        device.Functions.Add(
            new Loris.Models.Function
            {
                DeviceFunctionId = 2,
                Name = "set_led_green",
                Description = "Brightness: 0-100",
                ArgType = FuncArgType.Int,
            }
        );

        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync();

        return device;
    }

    private static async Task SeedValidAutomations(LorisDbContext ctx)
    {
        await ctx.Automations.ExecuteDeleteAsync();

        var automations = new List<Automation>
        {
            new()
            {
                AutomationId = Guid.Parse("42e40ec4-71d9-426c-9fe5-f6eda4f52928"),
                Name = "Another Automation",
                Triggers =
                    """[{"$type":"sensor-crossed","deviceName":"loris_friendly","sensorName":"env_temp_sensor","value":"25","topBottom":false}]""",
                Conditions = "[]",
                Actions =
                    """[{"$type":"device-call","serviceKey":"sensor","deviceName":"loris_friendly","functionName":"set_led_green","value":"50"}]""",
            },
            new()
            {
                AutomationId = Guid.Parse("42e40ec4-71d9-426c-90e5-f5eda4f52928"),
                Name = "Another Automation",
                Triggers =
                    """[{"$type":"sensor-crossed","deviceName":"loris_friendly","sensorName":"env_temp_sensor","value":"25","topBottom":true}]""",
                Conditions = "[]",
                Actions =
                    """[{"$type":"device-call","deviceName":"loris_friendly","functionName":"set_led_green","value":"0"}]""",
            },
        };

        ctx.Automations.AddRange(automations);
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedAutomationsWithOneInvalid(LorisDbContext ctx)
    {
        await ctx.Automations.ExecuteDeleteAsync();

        var automations = new List<Automation>
        {
            new()
            {
                AutomationId = Guid.Parse("42e40ec4-71d9-426c-9fe5-f6eda4f52928"),
                Name = "Another Automation",
                Triggers =
                    """[{"$type":"sensor-crossed","deviceName":"THIS IS INVALID NAME","sensorName":"env_temp_sensor","value":"25","topBottom":false}]""",
                Conditions = "[]",
                Actions =
                    """[{"$type":"device-call","serviceKey":"sensor","deviceName":"loris_friendly","functionName":"set_led_green","value":"50"}]""",
            },
            new()
            {
                AutomationId = Guid.Parse("42e40ec4-71d9-426c-90e5-f5eda4f52928"),
                Name = "Another Automation",
                Triggers =
                    """[{"$type":"sensor-crossed","deviceName":"loris_friendly","sensorName":"env_temp_sensor","value":"25","topBottom":true}]""",
                Conditions = "[]",
                Actions =
                    """[{"$type":"device-call","deviceName":"loris_friendly","functionName":"set_led_green","value":"0"}]""",
            },
        };

        ctx.Automations.AddRange(automations);
        await ctx.SaveChangesAsync();
    }

    private sealed class FakedIndex : IAutomationIndex
    {
        public Dictionary<IEvent, List<CompiledAutomation>> _index = [];

        public bool TryGet(
            IEvent key,
            [MaybeNullWhen(false)] out IReadOnlyList<CompiledAutomation> automations
        )
        {
            var hit = _index.TryGetValue(key, out List<CompiledAutomation>? list);
            automations = list;
            return hit;
        }

        public void Update(Dictionary<IEvent, List<CompiledAutomation>> index) => _index = index;
    }
}
