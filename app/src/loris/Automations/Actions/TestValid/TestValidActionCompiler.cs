using System.Collections.ObjectModel;
using Loris.Types;

namespace Loris.Automations.Actions.TestValid;

public class TestValidActionCompiler : ActionCompilerBase<TestValidActionDto>
{
    protected override Action Compile(TestValidActionDto dto, LookupContext? lookup) =>
        () => Console.WriteLine("The test has completed successfuly");

    protected override ValueTask<bool> TryValidateAsync(
        TestValidActionDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken
    )
    {
        lookup = default;
        return ValueTask.FromResult(true);
    }
}
