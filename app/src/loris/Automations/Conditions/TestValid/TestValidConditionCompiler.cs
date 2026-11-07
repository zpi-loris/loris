using Loris.Data;

namespace Loris.Automations.Conditions.TestValid;

public class TestValidConditionCompiler : ConditionCompilerBase<TestValidConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        TestValidConditionDto dto,
        LookupContext? lookup
    ) => (_) => true;

    protected override async Task<ValidationResult> ValidateAsync(
        TestValidConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        return ValidationResult.Success();
    }
}
