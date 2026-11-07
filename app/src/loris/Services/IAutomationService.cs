using Loris.Automations;
using Loris.Automations.Models;

namespace Loris.Services;

public interface IAutomationService
{
    public Task<IReadOnlyList<AutomationError>> ValidateAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
    public IReadOnlyList<Automation> CompileAutomations(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
    public void ApplyAutomations(IReadOnlyList<Automation> automations);
    public Task SaveAutomationRulesToDiskAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
}
