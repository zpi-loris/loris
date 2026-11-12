using System.Threading.Channels;
using Loris.Data;
using Loris.Events;
using Loris.Messages;

namespace Loris.Consumers;

public class DeviceConsumer(
    IStateCache stateCache,
    ILogger<DeviceConsumer> logger,
    ChannelReader<IDeviceMessage> deviceChannelReader,
    ChannelWriter<IEventMessage> eventChannelWriter,
    ChannelWriter<IDbWriteMessage> dbWriteChannel
) : BackgroundService
{
    private readonly IStateCache _stateCache = stateCache;
    private readonly ILogger<DeviceConsumer> _logger = logger;
    private readonly ChannelReader<IDeviceMessage> _deviceChannelReader = deviceChannelReader;
    private readonly ChannelWriter<IEventMessage> _eventChannelWriter = eventChannelWriter;
    private readonly ChannelWriter<IDbWriteMessage> _dbWriteChannel = dbWriteChannel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var deviceMessage = await _deviceChannelReader.ReadAsync(stoppingToken);

            if (deviceMessage is UplinkMessage uplinkMessage)
            {
                await HandleUplink(uplinkMessage, stoppingToken);
            }
        }
    }

    private async Task HandleUplink(UplinkMessage uplinkMessage, CancellationToken stoppingToken)
    {
        switch (uplinkMessage.Payload.PayloadCase)
        {
            case Protocol.Uplink.PayloadOneofCase.Report:
                if (_stateCache.IsDeviceAccepted(uplinkMessage.DevEUI))
                {
                    // write new event
                    await _eventChannelWriter.WriteAsync(
                        new SensorEventMessage()
                        {
                            Event = new SensorEvent()
                            {
                                DevEui = uplinkMessage.DevEUI,
                                SensorID = uplinkMessage.Payload.Report.SensorId,
                            },
                            Previous = _stateCache.TryGetSensorLastState(
                                uplinkMessage.DevEUI,
                                uplinkMessage.Payload.Report.SensorId,
                                out var previus
                            )
                                ? previus
                                : default,
                            Current = uplinkMessage.Payload.Report,
                        },
                        stoppingToken
                    );

                    // write to database

                    // update sensors cache
                    _stateCache.UpdateSensors(
                        uplinkMessage.DevEUI,
                        uplinkMessage.Payload.Report,
                        uplinkMessage.DateTime
                    );
                }
                break;
            case Protocol.Uplink.PayloadOneofCase.Capabilities:
                break;
            case Protocol.Uplink.PayloadOneofCase.Ping:
                break;

            // these we ignore
            case Protocol.Uplink.PayloadOneofCase.None:
            default:
                break;
        }
    }
}
