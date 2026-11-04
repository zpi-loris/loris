namespace Loris.Automations.Actions.Test;

public class TestActionService : IActionService
{
    public Action CompileAction(IActionDto dto)
    {
        if (dto is TestActionDto value)
        {
            return () => Console.WriteLine("The test has completed successfuly");
        }
        else
            throw new ArgumentException($"Value is not TestActionDto: {dto}");
    }
}
