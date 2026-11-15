using System.Diagnostics.CodeAnalysis;
using Loris.Automations.Models;
using Loris.Events;

namespace Loris.Data;

public interface IAutomationIndex
{
    public bool TryGet(
        IEvent key,
        [MaybeNullWhen(false)] out IReadOnlyList<Automation> automations
    );

    public void Update(Dictionary<IEvent, List<Automation>> index);
}
