using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.Test;

public class TestTrigger : ITrigger
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger()
    {
        return (TestEvent.Default, (_) => true);
    }

    public Task ValidateAsync(IServiceProvider sp, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
