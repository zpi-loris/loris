using Loris.Automations;

namespace Loris.Services;

public interface IAutomationService
{
    public Task ValidateAutomationsAsync(IReadOnlyList<AutomationRuleDto> rules);
    public Task<IReadOnlyList<Automation>> CompileAutomationsAsync(
        IReadOnlyList<AutomationRuleDto> rules
    );
    public void ApplyAutomations(IReadOnlyList<Automation> automations);
    public Task SaveAutomationRulesToDiskAsync(IReadOnlyList<AutomationRuleDto> rules);
}
