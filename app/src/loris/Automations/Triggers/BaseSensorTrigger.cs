using Loris.Events;
using Loris.Messages;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Triggers;

public abstract class BaseSensorTrigger : ITrigger
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
