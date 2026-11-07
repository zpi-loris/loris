using System.Collections.ObjectModel;
using Loris.Automations.conditions;
using Loris.Data;
using Loris.Types;

namespace Loris.Automations.Conditions.TestValid;

public class TestValidConditionCompiler : ConditionCompilerBase<TestValidConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        TestValidConditionDto dto,
        LookupContext? lookup
    ) => (_) => true;

    protected override ValueTask<bool> TryValidateAsync(
        TestValidConditionDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken
    )
    {
        lookup = default;
        return ValueTask.FromResult(true);
    }
}
