using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.TestValid;

public class TestValidTriggerCompiler : TriggerCompilerBase<TestValidTriggerDto>
{
    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TestValidTriggerDto dto,
        LookupContext? lookup
    ) => (TestEvent.Default, (_) => true);

    protected override async Task<ValidationResult> ValidateAsync(
        TestValidTriggerDto dto,
        CancellationToken cancellationToken
    ) => ValidationResult.Success();
}
