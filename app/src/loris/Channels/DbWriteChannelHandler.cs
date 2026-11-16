using Loris.Data;
using Loris.Extensions;
using Loris.Logging;
using Loris.Messages.Database;
using Loris.Models;
using Microsoft.EntityFrameworkCore;
using Moniker;

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
            ReportWriteMessage sensor => HandleReport(sensor, stoppingToken),
            CapabilitiesWriteMessage capabilities => HandleCapabilities(
                capabilities,
                stoppingToken
            ),
            PingWriteMessage ping => HandlePing(ping, stoppingToken),
            _ => Task.CompletedTask,
        };
    }

    private async Task HandleCapabilities(
        CapabilitiesWriteMessage message,
        CancellationToken stoppingToken
    )
    {
        var devEui = message.DevEui;
        var datetime = message.DateTime;
        var capabilities = message.Capabilities;

        using var ctx = await _ctxFactory.CreateDbContextAsync(stoppingToken);

        // check if the device already exists
        if (await ctx.TryBumpDeviceAsync(devEui, datetime, stoppingToken))
        {
            _logger.DeviceAlreadyExists(devEui, datetime);

            // update the device status to PENDING if it was previously DISCONNECTED
            var updatedRows = await ctx
                .Devices.Where(d => d.DevEui == devEui)
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
            LastSeen = datetime,
            DeviceStatus = DeviceStatus.PENDING,
        };

        foreach (var sensor in capabilities.Sensors)
        {
            newDevice.Sensors.Add(Sensor.FromProto(devEui, sensor));
        }

        foreach (var function in capabilities.Functions)
        {
            newDevice.Functions.Add(Function.FromProto(devEui, function));
        }

        _logger.NewDeviceAdded(devEui, newDevice.Sensors.Count, newDevice.Functions.Count);

        ctx.Devices.Add(newDevice);
        await ctx.SaveChangesAsync(stoppingToken);
    }

    private async Task HandleReport(ReportWriteMessage message, CancellationToken stoppingToken)
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(stoppingToken);

        if (!await ctx.TryBumpDeviceAsync(message.DevEui, message.DateTime, stoppingToken))
        {
            _logger.DeviceNotFound(message.DevEui, message.DateTime);
            return;
        }

        ctx.SensorReadings.Add(
            SensorReading.FromProto(message.DevEui, message.DateTime, message.Report)
        );
        await ctx.SaveChangesAsync(stoppingToken);
    }

    private async Task HandlePing(PingWriteMessage message, CancellationToken stoppingToken)
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(stoppingToken);

        _logger.ReceivedPing(message.DevEui, message.DateTime);

        await ctx.TryBumpDeviceAsync(message.DevEui, message.DateTime, stoppingToken);
    }
}
