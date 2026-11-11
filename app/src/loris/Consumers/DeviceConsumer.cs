using System.Threading.Channels;
using Loris.Messages;

namespace Loris.Consumers;

public class DeviceConsumer(
    ChannelReader<IDeviceMessage> deviceChannelReader,
    ChannelWriter<IEventMessage> eventChannelWriter,
    ChannelWriter<IDbWriteMessage> dbWriteChannel,
    ILogger<DeviceConsumer> logger
) : BackgroundService
{
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
                HandleUplink(uplinkMessage);
            }
        }
    }

    private void HandleUplink(UplinkMessage uplinkMessage)
    {
        switch (uplinkMessage.Payload.PayloadCase)
        {
            case Protocol.Uplink.PayloadOneofCase.Report:
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
