using System.Threading.Channels;
using Loris.Channels;
using Loris.Messages.Database;

namespace Loris.Consumers;

public class DbWriteChannelConsumer(
    ILogger<DbWriteChannelConsumer> logger,
    IDbWriteChannelHandler dbWriteChannelHandler,
    ChannelReader<IDbWriteMessage> dbWriteChannelReader
) : BackgroundService
{
    // TODO: consider paralleling DeviceConsumer

    private readonly ILogger<DbWriteChannelConsumer> _logger = logger;
    private readonly IDbWriteChannelHandler _dbWriteChannelHandler = dbWriteChannelHandler;
    private readonly ChannelReader<IDbWriteMessage> _dbWriteChannelReader = dbWriteChannelReader;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var deviceMessage = await _dbWriteChannelReader.ReadAsync(stoppingToken);

            _logger.LogDebug("new db write Message received on db write channel");

            await _dbWriteChannelHandler.HandleAsync(deviceMessage, stoppingToken);
        }
    }
}
