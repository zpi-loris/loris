using System.Collections.ObjectModel;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.Test;

public class TestTriggerCompiler : TriggerCompilerBase<TestTriggerDto>
{
    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TestTriggerDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => throw new ArgumentException("Testing exception");

    // public (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
    //     ITriggerDto dto,
    //     ReadOnlyDictionary<string, object> lookup
    // )
    // {
    //     if (dto is TestTriggerDto value)
    //     {
    //         return (TestEvent.Default, (_) => true);
    //     }
    //     else
    //         throw new ArgumentException($"value is not TestTriggerDto: {dto}");
    // }

    protected override ValueTask<bool> TryValidateAsync(
        TestTriggerDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(true);
    }
}
