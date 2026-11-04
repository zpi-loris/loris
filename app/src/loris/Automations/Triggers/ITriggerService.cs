using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers;

public interface ITriggerService
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger(ITriggerDto dto);
}
