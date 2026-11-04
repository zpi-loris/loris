using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;

namespace Loris.Automations;

public record AutomationRuleDto(
    Guid Id,
    string Name,
    IReadOnlyList<ITriggerDto> Triggers,
    IReadOnlyList<IConditionDto> Conditions,
    IReadOnlyList<IActionDto> Actions
) { }
