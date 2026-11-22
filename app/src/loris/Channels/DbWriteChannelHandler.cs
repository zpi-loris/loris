using Loris.Data;
using Loris.Extensions;
using Loris.Logging;
using Loris.Messages.Database;
using Loris.Models;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;
using Moniker;
using Function = Loris.Models.Function;
using Sensor = Loris.Models.Sensor;

namespace Loris.Channels;

public class DbWriteChannelHandler(
    ILogger<DbWriteChannelHandler> logger,
    IDbContextFactory<LorisDbContext> ctxFactory
) : IDbWriteChannelHandler
{
    private readonly ILogger<DbWriteChannelHandler> _logger = logger;
    private readonly IDbContextFactory<LorisDbContext> _ctxFactory = ctxFactory;

    public Task HandleAsync(IDbWriteMessage message, CancellationToken stoppingToken)
    {
        return message switch
        {
            ReportWriteMessage sensorMessage => HandleReport(
                sensorMessage.SensorId,
                sensorMessage.DevEui,
                sensorMessage.Report,
                sensorMessage.DateTime,
                stoppingToken
            ),
            CapabilitiesWriteMessage capabilitiesMessage => HandleCapabilities(
                capabilitiesMessage.DevEui,
                capabilitiesMessage.Capabilities,
                capabilitiesMessage.DateTime,
                stoppingToken
            ),
            PingWriteMessage pingMessage => HandlePing(
                pingMessage.DevEui,
                pingMessage.DateTime,
                stoppingToken
            ),
            _ => Task.CompletedTask,
        };
    }

    // TODO ANALYZE THIS FUNCTION, IF IT ADHERES TO
    // THE DEVEUI / DEVICE ID LOGIC
    private async Task HandleCapabilities(
        DevEui devEui,
        Capabilities capabilities,
        DateTime dateTime,
        CancellationToken stoppingToken
    )
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(stoppingToken);

        // check if the device already exists
        if (await ctx.TryBumpDeviceAsync(devEui, dateTime, stoppingToken))
        {
            _logger.DeviceAlreadyExists(devEui, dateTime);

            // update the device status to PENDING if it was previously DISCONNECTED
            var updatedRows = await ctx
                .Devices.Where(d => d.DevEui == devEui)
                .Where(d => d.DeviceStatus == DeviceStatus.DISCONNECTED)
                .ExecuteUpdateAsync(
                    setter => setter.SetProperty(p => p.DeviceStatus, DeviceStatus.PENDING),
                    stoppingToken
                );

            if (updatedRows > 0)
            {
                _logger.DisconnectedIsPending(devEui);
            }

            // early return since the device already exists
            return;
        }

        var newDevice = new Device()
        {
            DevEui = devEui,
            DeviceName = capabilities.DeviceName,
            FriendlyName = $"{NameGenerator.GenerateMoniker()}{DateTime.Now:HHmmss}",
            LastSeen = dateTime,
            DeviceStatus = DeviceStatus.PENDING,
        };

        foreach (var sensor in capabilities.Sensors)
        {
            newDevice.Sensors.Add(Sensor.FromProto(newDevice.DeviceId, sensor));
        }

        foreach (var function in capabilities.Functions)
        {
            newDevice.Functions.Add(Function.FromProto(newDevice.DeviceId, function));
        }

        _logger.NewDeviceAdded(devEui, newDevice.Sensors.Count, newDevice.Functions.Count);

        ctx.Devices.Add(newDevice);
        await ctx.SaveChangesAsync(stoppingToken);
    }

    private async Task HandleReport(
        Guid sensorId,
        DevEui devEui,
        Report report,
        DateTime dateTime,
        CancellationToken stoppingToken
    )
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(stoppingToken);

        if (!await ctx.TryBumpDeviceAsync(devEui, dateTime, stoppingToken))
        {
            _logger.DeviceNotFound(devEui, dateTime);
            return;
        }

        ctx.SensorReadings.Add(SensorReading.FromProto(sensorId, dateTime, report));
        await ctx.SaveChangesAsync(stoppingToken);
    }

    private async Task HandlePing(DevEui devEui, DateTime dateTime, CancellationToken stoppingToken)
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(stoppingToken);

        _logger.ReceivedPing(devEui, dateTime);

        await ctx.TryBumpDeviceAsync(devEui, dateTime, stoppingToken);
    }
}
