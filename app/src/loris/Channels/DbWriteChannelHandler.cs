using Loris.Messages.Database;

namespace Loris.Channels;

public class DbWriteChannelHandler : IDbWriteChannelHandler
{
    public Task HandleAsync(IDbWriteMessage message, CancellationToken stoppingToken)
    {
        return message switch
        {
            SensorWriteMessage sensor => HandleSensor(sensor, stoppingToken),
            CapabilitiesWriteMessage capabilities => HandleCapabilities(
                capabilities,
                stoppingToken
            ),
            PingWriteMessage ping => HandlePing(ping, stoppingToken),
            _ => Task.CompletedTask,
        };
    }

    private Task HandleCapabilities(
        CapabilitiesWriteMessage message,
        CancellationToken stoppingToken
    )
    {
        throw new NotImplementedException();
    }

    private Task HandleSensor(SensorWriteMessage message, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }

    private Task HandlePing(PingWriteMessage message, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
