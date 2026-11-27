using Loris.Data;

namespace Loris.Automations.Conditions.Example;

public class ExampleConditionCompiler : ConditionCompilerBase<ExampleConditionDto>
{
    // see Triggers/Example for full example
    protected override Func<IStateCache, bool> Compile(
        ExampleConditionDto dto,
        LookupContext? lookup
    ) => (_) => true;

    // see Triggers/Example for full example
    protected override async Task<BlockValidationResult> ValidateAsync(
        ExampleConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        return BlockValidationResult.Success();
    }
}
