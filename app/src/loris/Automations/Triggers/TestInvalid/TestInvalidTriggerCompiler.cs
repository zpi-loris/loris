using System.Collections.ObjectModel;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.TestInvalid;

public class TestInvalidTriggerCompiler : TriggerCompilerBase<TestInvalidTriggerDto>
{
    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TestInvalidTriggerDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => throw new ArgumentException("Testing exception");

    protected override ValueTask<bool> TryValidateAsync(
        TestInvalidTriggerDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(false);
    }
}
