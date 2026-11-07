using System.Collections.ObjectModel;
using Loris.Types;

namespace Loris.Automations.Actions.TestInvalid;

public class TestInvalidActionCompiler : ActionCompilerBase<TestInvalidActionDto>
{
    protected override Action Compile(TestInvalidActionDto dto, LookupContext? lookup) =>
        throw new ArgumentException("Testing exception");

    protected override async Task<ValidationResult> ValidateAsync(
        TestInvalidActionDto dto,
        CancellationToken cancellationToken
    ) => ValidationResult.Failure([new BlockError(nameof(dto.Name), dto.Name, "invalid")]);
}
