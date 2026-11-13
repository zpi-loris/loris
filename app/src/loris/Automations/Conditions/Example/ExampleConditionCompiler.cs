using Loris.Data;

namespace Loris.Automations.Conditions.Example;

public class ExampleConditionCompiler : ConditionCompilerBase<ExampleConditionDto>
{
    // see Triggers/Example for full example
    protected override Func<StateCache, bool> Compile(
        ExampleConditionDto dto,
        LookupContext? lookup
    ) => (_) => true;

    // see Triggers/Example for full example
    protected override async Task<ValidationResult> ValidateAsync(
        ExampleConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        return ValidationResult.Success();
    }
}
