using Loris.Data;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Models;

public record Automation(
    Guid Id,
    string Name,
    IEvent Trigger,
    Func<IEventMessage, bool> TriggerFilter,
    IReadOnlyList<Func<IStateCache, bool>> Conditions,
    IReadOnlyList<Action> Actions
) { }
