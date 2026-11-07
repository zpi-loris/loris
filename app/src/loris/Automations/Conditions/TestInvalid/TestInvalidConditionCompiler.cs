using System.Collections.ObjectModel;
using Loris.Data;
using Loris.Types;

namespace Loris.Automations.Conditions.TestInvalid;

public class TestInvalidConditionCompiler : ConditionCompilerBase<TestInvalidConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        TestInvalidConditionDto dto,
        LookupContext? lookup
    ) => throw new ArgumentException("Testing exception");

    protected override async Task<ValidationResult> ValidateAsync(
        TestInvalidConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        return ValidationResult.Failure([new(dto.Name, "invalid")]);
    }
}
