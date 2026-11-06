using System.Collections.ObjectModel;
using Loris.Automations.conditions;
using Loris.Data;

namespace Loris.Automations.Conditions.TestInvalid;

public class TestInvalidConditionCompiler : ConditionCompilerBase<TestInvalidConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        TestInvalidConditionDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => throw new ArgumentException("Testing exception");

    protected override ValueTask<bool> TryValidateAsync(
        TestInvalidConditionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(false);
    }
}
