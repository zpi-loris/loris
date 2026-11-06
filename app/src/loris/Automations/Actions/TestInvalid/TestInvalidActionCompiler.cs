using System.Collections.ObjectModel;

namespace Loris.Automations.Actions.TestInvalid;

public class TestInvalidActionCompiler : ActionCompilerBase<TestInvalidActionDto>
{
    protected override Action Compile(
        TestInvalidActionDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => throw new ArgumentException("Testing exception");

    protected override ValueTask<bool> TryValidateAsync(
        TestInvalidActionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(false);
    }
}
