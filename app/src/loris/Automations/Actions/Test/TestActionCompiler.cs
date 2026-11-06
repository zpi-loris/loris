using System.Collections.ObjectModel;

namespace Loris.Automations.Actions.Test;

public class TestActionCompiler : IActionCompiler
{
    public Action Compile(IActionDto dto, ReadOnlyDictionary<string, object> lookup) =>
        throw new ArgumentException("Testing exception");

    // public Action Compile(IActionDto dto, ReadOnlyDictionary<string, object> lookup)
    // {
    //     if (dto is TestActionDto value)
    //     {
    //         return () => Console.WriteLine("The test has completed successfuly");
    //     }
    //     else
    //         throw new ArgumentException($"Value is not TestActionDto: {dto}");
    // }

    public ValueTask<bool> TryValidateAsync(
        IActionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(true);
    }
}
