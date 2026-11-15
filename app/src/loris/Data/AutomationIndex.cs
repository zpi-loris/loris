using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Loris.Automations.Models;
using Loris.Events;

namespace Loris.Data;

public sealed class AutomationIndex : IAutomationIndex
{
    private FrozenDictionary<IEvent, List<Automation>> _index = FrozenDictionary<
        IEvent,
        List<Automation>
    >.Empty;

    public bool TryGet(IEvent key, [MaybeNullWhen(false)] out IReadOnlyList<Automation> automations)
    {
        var hit = _index.TryGetValue(key, out List<Automation>? list);
        automations = list;
        return hit;
    }

    public void Update(Dictionary<IEvent, List<Automation>> index)
    {
        _index = index.ToFrozenDictionary();
    }
}
