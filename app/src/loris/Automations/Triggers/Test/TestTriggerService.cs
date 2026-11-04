using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.Test;

public class TestTriggerService : ITriggerService
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger(ITriggerDto dto)
    {
        if (dto is TestTriggerDto value)
        {
            return (TestEvent.Default, (_) => true);
        }
        else
            throw new ArgumentException($"value is not TestTriggerDto: {dto}");
    }
}
