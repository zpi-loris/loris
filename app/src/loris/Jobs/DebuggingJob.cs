namespace Loris.Jobs;

public class DebuggingJob(ILogger<DebuggingJob> logger) : IHostedService
{
    private readonly ILogger<DebuggingJob> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("IHostedService Debugging job STARTS");
        await Task.Delay(2000, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("IHostedService Debugging job ENDS");
        await Task.Delay(2000, cancellationToken);
    }
}
