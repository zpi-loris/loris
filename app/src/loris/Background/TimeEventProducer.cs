using System.Threading.Channels;
using System.Timers;
using Loris.Events;
using Loris.Logging;
using Loris.Messages.Event;

namespace Loris.Background;

public sealed class TimeEventProducer(
    ILogger<TimeEventProducer> logger,
    ChannelWriter<IEventMessage> channelWriter
) : IHostedService, IDisposable
{
    private readonly ChannelWriter<IEventMessage> _channelWriter = channelWriter;
    private readonly ILogger<TimeEventProducer> _logger = logger;

    private System.Timers.Timer _timer = null!;
    private CancellationTokenSource _stoppingCts = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _timer = new();
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = false;
        ResetTimer();

        return Task.CompletedTask;
    }

    static double GetInterval()
    {
        DateTime now = DateTime.Now;
        return (60 - now.Second) * 1000;
    }

    private void ResetTimer()
    {
        _timer.Interval = GetInterval();
        _timer.Start();
    }

    // async void on purpose, fire timer event and forget
    // we need to catch all the exceptions because unhandled will crash the process
    private async void OnTimedEvent(object? source, ElapsedEventArgs e)
    {
        try
        {
            if (!_stoppingCts.IsCancellationRequested)
            {
                ResetTimer();
                var time = e.SignalTime;
                await _channelWriter.WriteAsync(
                    new TimeEventMessage(TimeEvent.FromDateTime(time)),
                    _stoppingCts.Token
                );
            }
        }
        catch (Exception ex)
        {
            _logger.TimedEventError(ex);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();
        _timer.Elapsed -= OnTimedEvent;
        _timer.Stop();
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _timer.Dispose();
    }
}
