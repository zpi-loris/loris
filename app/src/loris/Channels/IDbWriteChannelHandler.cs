using Loris.Messages.Database;

namespace Loris.Channels;

public interface IDbWriteChannelHandler
{
    public Task HandleAsync(IDbWriteMessage message, CancellationToken stoppingToken);
}
