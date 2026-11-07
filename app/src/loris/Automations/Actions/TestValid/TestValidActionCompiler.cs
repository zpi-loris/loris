namespace Loris.Automations.Actions.TestValid;

public class TestValidActionCompiler : ActionCompilerBase<TestValidActionDto>
{
    protected override Action Compile(TestValidActionDto dto, LookupContext? lookup) =>
        () => Console.WriteLine("The test has completed successfuly");

    protected override async Task<ValidationResult> ValidateAsync(
        TestValidActionDto dto,
        CancellationToken cancellationToken
    ) => ValidationResult.Success();
}
