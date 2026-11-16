using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Loris.Automations;
using Loris.Events;

namespace Loris.Data;

public sealed class AutomationIndex : IAutomationIndex
{
    private FrozenDictionary<IEvent, List<CompiledAutomation>> _index = FrozenDictionary<
        IEvent,
        List<CompiledAutomation>
    >.Empty;

    public bool TryGet(
        IEvent key,
        [MaybeNullWhen(false)] out IReadOnlyList<CompiledAutomation> automations
    )
    {
        var hit = _index.TryGetValue(key, out List<CompiledAutomation>? list);
        automations = list;
        return hit;
    }

    public void Update(Dictionary<IEvent, List<CompiledAutomation>> index)
    {
        _index = index.ToFrozenDictionary();
    }
}
