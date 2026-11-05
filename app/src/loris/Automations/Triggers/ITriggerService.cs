using System.Collections.ObjectModel;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers;

public interface ITriggerService
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger(
        ITriggerDto dto,
        ReadOnlyDictionary<string, object> lookup
    );

    public bool TryValidate(ITriggerDto dto, out ReadOnlyDictionary<string, object> lookup);
}
