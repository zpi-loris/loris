using System.Threading.Channels;
using Loris.Channels;
using Loris.Data;
using Loris.Messages.Device;
using Loris.Messages.Event;

namespace Loris.Consumers;

public sealed class EventChannelConsumer(
    ILogger<EventChannelConsumer> logger,
    ChannelReader<IEventMessage> channelReader,
    IEventChannelHandler handler
) : IHostedService, IDisposable
{
    private readonly List<Task> _tasks = [];
    private CancellationTokenSource _stoppingCts = null!;

    private readonly ILogger<EventChannelConsumer> _logger = logger;
    private readonly ChannelReader<IEventMessage> _channelReader = channelReader;
    private readonly IEventChannelHandler _handler = handler;

    private readonly int _concurrency = Environment.ProcessorCount;

    // start <processor-count> channel handlers
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        for (int i = 0; i < _concurrency; i++)
        {
            _tasks.Add(Task.Run(() => ExecuteAsync(_stoppingCts.Token), _stoppingCts.Token));
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tasks.Count > 0)
        {
            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                foreach (var task in _tasks)
                {
                    await task.WaitAsync(cancellationToken)
                        .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            }
        }
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var eventMessage = await _channelReader.ReadAsync(stoppingToken);
            await _handler.HandleAsync(eventMessage, stoppingToken);
        }
    }
}
