using System.Collections.ObjectModel;
using Loris.Data;

namespace Loris.Automations.Conditions.Test;

public class TestConditionCompiler : IConditionCompiler
{
    public Func<StateCache, bool> Compile(
        IConditionDto dto,
        ReadOnlyDictionary<string, object> lookup
    ) => throw new ArgumentException("Testing exception");

    // public Func<StateCache, bool> Compile(
    //     IConditionDto dto,
    //     ReadOnlyDictionary<string, object> lookup
    // )
    // {
    //     if (dto is TestConditionDto value)
    //     {
    //         return (_) => true;
    //     }
    //     else
    //         throw new ArgumentException($"Value is not TestConditionDto: {dto}");
    // }

    public ValueTask<bool> TryValidateAsync(
        IConditionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return ValueTask.FromResult(true);
    }
}
