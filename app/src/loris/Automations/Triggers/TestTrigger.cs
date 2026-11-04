using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers
{
    public class TestTrigger : ITrigger
    {
        public required string Name { get; set; }

        public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger()
        {
            return (TestEvent.Default, (_) => true);
        }

        public Task ValidateAsync(IServiceProvider sp, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
