using System.Threading.Channels;
using Loris.Data;
using Loris.Events;
using Loris.Messages.Database;
using Loris.Messages.Device;
using Loris.Messages.Event;
using Loris.Protocol;

namespace Loris.Channels;

public class DeviceChannelHandler(
    IStateCache stateCache,
    ILogger<DeviceChannelHandler> logger,
    ChannelWriter<IEventMessage> eventChannelWriter,
    ChannelWriter<IDbWriteMessage> dbWriteChannelWriter
) : IDeviceChannelHandler
{
    private readonly ILogger<DeviceChannelHandler> _logger = logger;
    private readonly IStateCache _stateCache = stateCache;
    private readonly ChannelWriter<IEventMessage> _eventChannelWriter = eventChannelWriter;
    private readonly ChannelWriter<IDbWriteMessage> _dbWriteChannel = dbWriteChannelWriter;

    public Task HandleAsync(IDeviceMessage message, CancellationToken stoppingToken)
    {
        return message switch
        {
            UplinkMessage uplinkMessage => HandleUplinkAsync(uplinkMessage, stoppingToken),
            _ => Task.CompletedTask,
        };
    }

    private Task HandleUplinkAsync(UplinkMessage uplinkMessage, CancellationToken stoppingToken)
    {
        return uplinkMessage.Payload.PayloadCase switch
        {
            Uplink.PayloadOneofCase.Report => HandleReportAsync(uplinkMessage, stoppingToken),
            Uplink.PayloadOneofCase.Capabilities => HandleCapabilitiesAsync(
                uplinkMessage,
                stoppingToken
            ),
            Uplink.PayloadOneofCase.Ping => HandlePingAsync(uplinkMessage, stoppingToken),
            _ => Task.CompletedTask,
        };
    }

    private async Task HandleReportAsync(
        UplinkMessage uplinkMessage,
        CancellationToken stoppingToken
    )
    {
        var devEui = uplinkMessage.DevEUI;

        // early drop if not accepted
        if (!_stateCache.IsDeviceAccepted(devEui))
        {
            // TODO: rewrite to static log expression
            _logger.LogWarning("Device was not accepted before, but sends reports");
            return;
        }

        var report = uplinkMessage.Payload.Report;
        var time = uplinkMessage.DateTime;

        // write new event
        await _eventChannelWriter.WriteAsync(
            new SensorEventMessage()
            {
                Event = new SensorEvent() { DevEui = devEui, SensorID = report.SensorId },
                Previous = _stateCache.TryGetSensorLastState(
                    devEui,
                    report.SensorId,
                    out var previus
                )
                    ? previus
                    : default,
                Current = report,
            },
            stoppingToken
        );

        // write to database
        await _dbWriteChannel.WriteAsync(
            new SensorWriteMessage()
            {
                DevEui = devEui,
                Report = report,
                DateTime = time,
            },
            stoppingToken
        );

        // update sensors cache
        _stateCache.UpdateSensors(devEui, report, time);
    }

    private async Task HandleCapabilitiesAsync(
        UplinkMessage uplinkMessage,
        CancellationToken stoppingToken
    )
    {
        var devEui = uplinkMessage.DevEUI;

        // early drop if already accepted (contrary to report)
        if (_stateCache.IsDeviceAccepted(devEui))
        {
            // TODO: rewrite to static log expression
            _logger.LogWarning("Device is already accepted");
            return;
        }

        var capabilities = uplinkMessage.Payload.Capabilities;
        var time = uplinkMessage.DateTime;

        // write to database
        await _dbWriteChannel.WriteAsync(
            new CapabilitiesWriteMessage()
            {
                DevEui = devEui,
                Capabilities = capabilities,
                DateTime = time,
            },
            stoppingToken
        );
    }

    private async Task HandlePingAsync(UplinkMessage uplinkMessage, CancellationToken stoppingToken)
    {
        var devEui = uplinkMessage.DevEUI;

        // early drop if not accepted
        if (!_stateCache.IsDeviceAccepted(devEui))
        {
            // TODO: rewrite to static log expression
            _logger.LogWarning("Device was not accepted before, but sends pings");
            return;
        }

        var time = uplinkMessage.DateTime;

        // write to database
        await _dbWriteChannel.WriteAsync(
            new PingWriteMessage() { DevEui = devEui, DateTime = time },
            stoppingToken
        );
    }
}
