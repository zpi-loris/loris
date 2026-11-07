using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Loris.Automations.Models;
using Loris.Events;

namespace Loris.Data;

public sealed class AutomationIndex
{
    private FrozenDictionary<IEvent, Automation> _index = FrozenDictionary<
        IEvent,
        Automation
    >.Empty;

    public bool TryGet(IEvent key, [MaybeNullWhen(false)] out Automation automation)
    {
        var hit = _index.TryGetValue(key, out var val);
        automation = val;
        return hit;
    }

    public void Update(Dictionary<IEvent, Automation> index)
    {
        _index = index.ToFrozenDictionary();
    }
}
