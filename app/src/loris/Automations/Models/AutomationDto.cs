using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;

namespace Loris.Automations.Models;

public record AutomationDto(
    Guid Id,
    string Name,
    IReadOnlyList<ITriggerDto> Triggers,
    IReadOnlyList<IConditionDto> Conditions,
    IReadOnlyList<IActionDto> Actions
) { }
