using Loris.Automations.Models;

namespace Loris.Services;

public interface IAutomationService
{
    public Task ValidateAutomationsAsync(
        IReadOnlyList<AutomationRuleDto> rules,
        CancellationToken cancellationToken = default
    );
    public Task<IReadOnlyList<Automation>> CompileAutomationsAsync(
        IReadOnlyList<AutomationRuleDto> rules,
        CancellationToken cancellationToken = default
    );
    public void ApplyAutomations(IReadOnlyList<Automation> automations);
    public Task SaveAutomationRulesToDiskAsync(
        IReadOnlyList<AutomationRuleDto> rules,
        CancellationToken cancellationToken = default
    );
}
