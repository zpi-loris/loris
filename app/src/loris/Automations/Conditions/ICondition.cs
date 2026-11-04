using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions;

public interface IConditionService
{
    public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition(IConditionDto dto);
}
