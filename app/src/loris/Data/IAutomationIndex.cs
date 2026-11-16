using System.Diagnostics.CodeAnalysis;
using Loris.Automations;
using Loris.Events;

namespace Loris.Data;

public interface IAutomationIndex
{
    public bool TryGet(
        IEvent key,
        [MaybeNullWhen(false)] out IReadOnlyList<CompiledAutomation> automations
    );

    public void Update(Dictionary<IEvent, List<CompiledAutomation>> index);
}
