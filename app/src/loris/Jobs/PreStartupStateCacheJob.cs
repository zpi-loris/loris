using Loris.Data;
using Loris.Models;
using Microsoft.EntityFrameworkCore;

namespace Loris.Jobs;

public class PreStartupStateCacheJob(
    ILogger<PreStartupStateCacheJob> logger,
    IServiceScopeFactory serviceScopeFactory,
    IStateCache state
) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = serviceScopeFactory;
    private readonly ILogger<PreStartupStateCacheJob> _logger = logger;
    private readonly IStateCache _state = state;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();

        var data = await ctx
            .Devices.Where(d => d.DeviceStatus == DeviceStatus.ACTIVE)
            .Include(d => d.Sensors)
                .ThenInclude(d => d.SensorReadings.OrderByDescending(r => r).Take(1))
            .ToListAsync(cancellationToken);

        foreach (var device in data)
        {
            _state.UpdateDevices(device);
            foreach (var sensor in device.Sensors)
            {
                if (sensor.SensorReadings.FirstOrDefault() is SensorReading reading)
                {
                    _state.UpdateSensors(device.DevEui, reading);
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
