namespace Loris.Automations.Actions.Test;

public class TestAction : IAction
{
    public Action CompileAction()
    {
        return () => Console.WriteLine("The test has completed successfuly");
    }

    public Task ValidateAsync(IServiceProvider sp, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
