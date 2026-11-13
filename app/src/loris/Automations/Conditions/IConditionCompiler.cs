using Loris.Data;

namespace Loris.Automations.Conditions;

public interface IConditionCompiler
    : IAutomationBlockCompiler<IConditionDto, Func<StateCache, bool>> { }
