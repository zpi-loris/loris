using System.Collections.ObjectModel;
using Loris.Events;
using Loris.Messages;
using Loris.Types;

namespace Loris.Automations.Triggers.TestInvalid;

public class TestInvalidTriggerCompiler : TriggerCompilerBase<TestInvalidTriggerDto>
{
    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TestInvalidTriggerDto dto,
        LookupContext? lookup
    ) => throw new ArgumentException("Testing exception");

    protected override ValueTask<bool> TryValidateAsync(
        TestInvalidTriggerDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken
    )
    {
        lookup = default;
        return ValueTask.FromResult(false);
    }
}
