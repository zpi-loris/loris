using Loris.Messages.Device;

namespace Loris.Channels;

public interface IDeviceChannelHandler
{
    public Task HandleAsync(IDeviceMessage message, CancellationToken stoppingToken);
}
