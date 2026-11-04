using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions;

public interface ICondition : IAutomationBlock
{
    public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition();
}
