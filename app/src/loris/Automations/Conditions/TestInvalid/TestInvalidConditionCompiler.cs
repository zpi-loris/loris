using System.Collections.ObjectModel;
using Loris.Automations.conditions;
using Loris.Data;
using Loris.Types;

namespace Loris.Automations.Conditions.TestInvalid;

public class TestInvalidConditionCompiler : ConditionCompilerBase<TestInvalidConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        TestInvalidConditionDto dto,
        LookupContext? lookup
    ) => throw new ArgumentException("Testing exception");

    protected override ValueTask<bool> TryValidateAsync(
        TestInvalidConditionDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken
    )
    {
        lookup = default;
        return ValueTask.FromResult(false);
    }
}
