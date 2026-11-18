using Loris.Automations;
using Loris.Results;

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
    public Task<Result> SaveToDiskAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );
}
