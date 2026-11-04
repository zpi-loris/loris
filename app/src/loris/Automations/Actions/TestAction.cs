namespace Loris.Automations.Actions
{
    public class TestAction : IAction
    {
        public required string Name { get; set; }

        public Action CompileAction()
        {
            return () => Console.WriteLine("The test has completed successfuly");
        }

        public Task ValidateAsync(IServiceProvider sp, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
