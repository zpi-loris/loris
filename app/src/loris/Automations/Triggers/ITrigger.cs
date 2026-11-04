using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers;

public interface ITrigger : IAutomationBlock
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger();
}
