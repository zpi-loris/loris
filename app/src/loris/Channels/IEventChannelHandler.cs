using Loris.Messages.Event;

namespace Loris.Channels;

public interface IEventChannelHandler
{
    public Task HandleAsync(IEventMessage message, CancellationToken stoppingToken);
}
