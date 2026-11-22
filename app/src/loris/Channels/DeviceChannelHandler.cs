using System.Threading.Channels;
using Loris.Data;
using Loris.Events;
using Loris.Logging;
using Loris.Messages.Database;
using Loris.Messages.Device;
using Loris.Messages.Event;
using Loris.Models;
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
            Uplink.PayloadOneofCase.Report => HandleReportAsync(
                uplinkMessage.DevEUI,
                uplinkMessage.Payload.Report,
                uplinkMessage.DateTime,
                stoppingToken
            ),
            Uplink.PayloadOneofCase.Capabilities => HandleCapabilitiesAsync(
                uplinkMessage.DevEUI,
                uplinkMessage.Payload.Capabilities,
                uplinkMessage.DateTime,
                stoppingToken
            ),
            Uplink.PayloadOneofCase.Ping => HandlePingAsync(
                uplinkMessage.DevEUI,
                uplinkMessage.DateTime,
                stoppingToken
            ),
            _ => Task.CompletedTask,
        };
    }

    private async Task HandleReportAsync(
        DevEui devEui,
        Report report,
        DateTime dateTime,
        CancellationToken stoppingToken
    )
    {
        _logger.ReportReceived(devEui, dateTime);

        // early drop if not accepted
        if (
            !_stateCache.TryGetDevice(devEui, out var device)
            || device.DeviceStatus != DeviceStatus.ACTIVE
        )
        {
            _logger.DeviceNotAccepted(devEui, "Report");
            return;
        }

        var sensor = _stateCache[(devEui, report.SensorId)];

        await WriteSensorEvent(report, device, sensor, stoppingToken);

        // write to database
        await _dbWriteChannel.WriteAsync(
            new ReportWriteMessage()
            {
                SensorId = sensor.SensorId,
                Report = report,
                DateTime = dateTime,
                DevEui = devEui,
            },
            stoppingToken
        );

        // update sensors cache
        _stateCache.AddOrUpdateReading(sensor.SensorId, report, dateTime);
    }

    private async Task WriteSensorEvent(
        Report report,
        Device device,
        Models.Sensor sensor,
        CancellationToken stoppingToken
    )
    {
        _stateCache.TryGetSensorLastReading(sensor.SensorId, out SensorReading? reading);
        await _eventChannelWriter.WriteAsync(
            new SensorEventMessage()
            {
                Event = new SensorEvent() { SensorId = sensor.SensorId },
                Previous = reading is not null ? reading.ToReport(report.SensorId) : default,
                Current = report,
            },
            stoppingToken
        );
    }

    private async Task HandleCapabilitiesAsync(
        DevEui devEui,
        Capabilities capabilities,
        DateTime dateTime,
        CancellationToken stoppingToken
    )
    {
        _logger.CapabilitiesReceived(devEui, dateTime);

        // early drop if already accepted (contrary to report)
        if (
            _stateCache.TryGetDevice(devEui, out var device)
            && device.DeviceStatus == DeviceStatus.ACTIVE
        )
        {
            _logger.DeviceAlreadyAccepted(devEui);
            return;
        }
        // write to database
        await _dbWriteChannel.WriteAsync(
            new CapabilitiesWriteMessage()
            {
                DevEui = devEui,
                Capabilities = capabilities,
                DateTime = dateTime,
            },
            stoppingToken
        );
    }

    private async Task HandlePingAsync(
        DevEui devEui,
        DateTime dateTime,
        CancellationToken stoppingToken
    )
    {
        _logger.PingReceived(devEui, dateTime);

        // early drop if not accepted
        if (
            !_stateCache.TryGetDevice(devEui, out var device)
            || device.DeviceStatus != DeviceStatus.ACTIVE
        )
        {
            _logger.DeviceNotAccepted(devEui, "Ping");
            return;
        }

        // write to database
        await _dbWriteChannel.WriteAsync(
            new PingWriteMessage() { DevEui = devEui, DateTime = dateTime },
            stoppingToken
        );
    }
}
