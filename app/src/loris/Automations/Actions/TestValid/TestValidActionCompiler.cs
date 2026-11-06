using System.Collections.ObjectModel;

namespace Loris.Automations.Actions.TestValid;

public class TestValidActionCompiler : ActionCompilerBase<TestValidActionDto>
{
    protected override Action Compile(
        TestValidActionDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => () => Console.WriteLine("The test has completed successfuly");

    protected override ValueTask<bool> TryValidateAsync(
        TestValidActionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(true);
    }
}
