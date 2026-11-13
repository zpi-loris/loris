using Loris.Automations;
using Loris.Automations.Models;

namespace Loris.Services;

public interface IAutomationService
{
    public Task<IReadOnlyList<AutomationError>> ValidateAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
    public Task<IReadOnlyList<AutomationError>> ApplyAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
    public Task SaveToDiskAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
}
