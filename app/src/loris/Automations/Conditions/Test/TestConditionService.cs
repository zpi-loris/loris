using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions.Test;

public class TestConditionService : IConditionService
{
    public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition(
        IConditionDto dto,
        ReadOnlyDictionary<string, object> lookup
    )
    {
        if (dto is TestConditionDto value)
        {
            return (_) => true;
        }
        else
            throw new ArgumentException($"Value is not TestConditionDto: {dto}");
    }

    public bool TryValidate(IConditionDto dto, out ReadOnlyDictionary<string, object> lookup)
    {
        lookup = new Dictionary<string, object>().AsReadOnly();
        return true;
    }
}
