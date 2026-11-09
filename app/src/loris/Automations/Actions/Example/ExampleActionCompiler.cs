namespace Loris.Automations.Actions.Example;

public class ExampleActionCompiler : ActionCompilerBase<ExampleActionDto>
{
    // see Triggers/Example for full example
    protected override Action Compile(ExampleActionDto dto, LookupContext? lookup) =>
        () => Console.WriteLine("This is an example action");

    // see Triggers/Example for full example
    protected override async Task<ValidationResult> ValidateAsync(
        ExampleActionDto dto,
        CancellationToken cancellationToken
    ) => ValidationResult.Success();
}
