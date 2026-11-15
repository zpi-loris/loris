namespace Loris.Automations.Actions.Example;

public class ExampleActionCompiler : ActionCompilerBase<ExampleActionDto>
{
    // see Triggers/Example for full example
    protected override Func<CancellationToken, Task> Compile(
        ExampleActionDto dto,
        LookupContext? lookup
    ) => (token) => Task.Run(() => Console.WriteLine("This is an example action"), token);

    // see Triggers/Example for full example
    protected override async Task<ValidationResult> ValidateAsync(
        ExampleActionDto dto,
        CancellationToken cancellationToken
    ) => ValidationResult.Success();
}
