using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.Sensor;

public abstract class BaseSensorTrigger
{
    public required string DeviceName { get; set; }

    public required string SensorName { get; set; }

    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger()
    {
        throw new NotImplementedException();
    }

    public Task ValidateAsync(IServiceProvider sp, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
