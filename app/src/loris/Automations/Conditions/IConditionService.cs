using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions;

public interface IConditionService
{
    public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition(
        IConditionDto dto,
        ReadOnlyDictionary<string, object> lookup
    );

    public bool TryValidate(IConditionDto dto, out ReadOnlyDictionary<string, object> lookup);
}
