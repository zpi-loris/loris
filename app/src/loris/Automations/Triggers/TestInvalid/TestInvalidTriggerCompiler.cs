using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers.TestInvalid;

public class TestInvalidTriggerCompiler : TriggerCompilerBase<TestInvalidTriggerDto>
{
    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TestInvalidTriggerDto dto,
        LookupContext? lookup
    ) => throw new ArgumentException("Testing exception");

    protected override async Task<ValidationResult> ValidateAsync(
        TestInvalidTriggerDto dto,
        CancellationToken cancellationToken
    ) => ValidationResult.Failure([new(nameof(dto.Name), dto.Name, "Invalid")]);
}
