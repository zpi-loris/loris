using System.Collections.ObjectModel;
using Loris.Types;

namespace Loris.Automations.Actions.TestInvalid;

public class TestInvalidActionCompiler : ActionCompilerBase<TestInvalidActionDto>
{
    protected override Action Compile(TestInvalidActionDto dto, LookupContext? lookup) =>
        throw new ArgumentException("Testing exception");

    protected override ValueTask<bool> TryValidateAsync(
        TestInvalidActionDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken
    )
    {
        lookup = default;
        return ValueTask.FromResult(false);
    }
}
