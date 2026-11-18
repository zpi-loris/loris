namespace Loris.Jobs;

public class PreStartupStateCacheJob : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
