using System.Threading.Channels;
using Loris.Channels;
using Loris.Messages.Device;

namespace Loris.Consumers;

// TODO: consider paralleling DeviceConsumer

public class DeviceChannelConsumer(
    ILogger<DeviceChannelConsumer> logger,
    IDeviceChannelHandler deviceChannelHandler,
    ChannelReader<IDeviceMessage> deviceChannelReader
) : BackgroundService
{
    private readonly ILogger<DeviceChannelConsumer> _logger = logger;
    private readonly IDeviceChannelHandler _deviceChannelHandler = deviceChannelHandler;
    private readonly ChannelReader<IDeviceMessage> _deviceChannelReader = deviceChannelReader;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var deviceMessage = await _deviceChannelReader.ReadAsync(stoppingToken);

            _logger.LogDebug("new device Message received on device channel");

            await _deviceChannelHandler.HandleAsync(deviceMessage, stoppingToken);
        }
    }
}
