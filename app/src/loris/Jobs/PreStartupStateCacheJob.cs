using Loris.Data;
using Loris.Logging;
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

    // TODO: ADD LOGGING
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.JobStarted(nameof(PreStartupStateCacheJob));

        await using var scope = _scopeFactory.CreateAsyncScope();

        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();

        var data = await ctx
            .Devices.Where(d => d.DeviceStatus == DeviceStatus.ACTIVE)
            .Include(d => d.Sensors)
                .ThenInclude(d => d.SensorReadings.OrderByDescending(r => r).Take(1))
            .AsSingleQuery()
            .ToListAsync(cancellationToken);

        foreach (var device in data)
        {
            _state[device.DevEui] = device;
            foreach (var sensor in device.Sensors)
            {
                if (sensor.SensorReadings.FirstOrDefault() is SensorReading reading)
                {
                    _state.AddOrUpdateReading(reading);
                }
            }
        }

        _logger.JobFinished(nameof(PreStartupStateCacheJob));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
