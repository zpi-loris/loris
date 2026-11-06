using Loris.Data;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations;

public record Automation(
    IEvent Trigger,
    Func<IEventMessage, bool> TriggerFilter,
    IReadOnlyList<Func<StateCache, bool>> Conditions,
    IReadOnlyList<Action> Actions
) { }
