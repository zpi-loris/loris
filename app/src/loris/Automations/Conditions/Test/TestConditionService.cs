using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions.Test;

public class TestConditionService : IConditionService
{
    public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition(IConditionDto dto)
    {
        if (dto is TestConditionDto value)
        {
            return (_) => true;
        }
        else
            throw new ArgumentException($"Value is not TestConditionDto: {dto}");
    }
}
