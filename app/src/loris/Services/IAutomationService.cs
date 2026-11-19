using Loris.Automations;

namespace Loris.Services;

public interface IAutomationService
{
    public Task<IReadOnlyList<AutomationError>> ValidateAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Applies the specified automation rules to the system.
    /// </summary>
    /// <param name="rules">
    /// The list of automation rules (DTO) to be applied.
    /// </param>
    /// <param name="force">
    /// A boolean flag indicating whether to force the application of the rules,
    /// even if some of them may have errors. If set to true, the service will attempt
    /// to apply only the valid rules and ignore the invalid ones.
    /// </param>
    /// <remarks>
    /// The force parameter is useful in scenario when the system is waking up<br/>
    /// and needs to apply the rules from the database/file. If by any chance some entities are missing,<br/>
    /// or altered, we still want to apply the valid rules and ignore the invalid ones.<br/>
    /// </remarks>
    public Task<IReadOnlyList<AutomationError>> ApplyAsync(
        IReadOnlyList<AutomationDto> rules,
        bool force = false,
        CancellationToken cancellationToken = default
    );
}
