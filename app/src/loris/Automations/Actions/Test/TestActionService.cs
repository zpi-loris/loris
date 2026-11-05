using System.Collections.ObjectModel;

namespace Loris.Automations.Actions.Test;

public class TestActionService : IActionService
{
    public Action CompileAction(IActionDto dto, ReadOnlyDictionary<string, object> lookup)
    {
        if (dto is TestActionDto value)
        {
            return () => Console.WriteLine("The test has completed successfuly");
        }
        else
            throw new ArgumentException($"Value is not TestActionDto: {dto}");
    }

    public bool TryValidate(IActionDto dto, out ReadOnlyDictionary<string, object> lookup)
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return true;
    }
}
