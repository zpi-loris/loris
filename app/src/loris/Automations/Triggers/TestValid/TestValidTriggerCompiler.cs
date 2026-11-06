using System.Collections.ObjectModel;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.TestValid;

public class TestValidTriggerCompiler : TriggerCompilerBase<TestValidTriggerDto>
{
    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TestValidTriggerDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => (TestEvent.Default, (_) => true);

    protected override ValueTask<bool> TryValidateAsync(
        TestValidTriggerDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(true);
    }
}
