using System.Collections.ObjectModel;
using Loris.Automations.conditions;
using Loris.Data;

namespace Loris.Automations.Conditions.TestValid;

public class TestValidConditionCompiler : ConditionCompilerBase<TestValidConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        TestValidConditionDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => (_) => true;

    protected override ValueTask<bool> TryValidateAsync(
        TestValidConditionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(true);
    }
}
