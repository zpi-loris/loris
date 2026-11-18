using Loris.Automations;
using Loris.Extensions;
using Loris.OpenApi;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]")]
[ApiController]
[ProducesUnauthenticated]
public class AutomationsController(IAutomationService service) : ControllerBase
{
    private readonly IAutomationService _service = service;

    [HttpPost]
    public async Task<IActionResult> Test(
        [FromBody] IReadOnlyList<AutomationDto> rules,
        CancellationToken ct
    )
    {
        var duplicateIds = rules.GroupBy(r => r.Id).Where(g => g.Count() > 1);
        if (duplicateIds.Any())
        {
            return this.DuplicateProblem(duplicateIds.SelectMany(s => s));
        }

        await _service.SaveToDiskAsync(rules, ct);

        return Ok();
    }

    /// <summary>
    /// Validate rules
    /// </summary>
    /// <remarks>
    /// Validates a list of automation rules, returning any validation errors found.
    /// This does not apply the rules yet
    /// </remarks>
    [HttpPost("validate")]
    [ProducesSuccessNoContent]
    [ProducesAutomationValidationError]
    [ProducesUnauthorized]
    public async Task<IActionResult> Validate(
        [FromBody] IReadOnlyList<AutomationDto> rules,
        CancellationToken ct
    )
    {
        var duplicateIds = rules.GroupBy(r => r.Id).Where(g => g.Count() > 1);
        if (duplicateIds.Any())
        {
            return this.DuplicateProblem(duplicateIds.SelectMany(s => s));
        }

        var errors = await _service.ValidateAsync(rules, ct);
        if (errors.Count > 0)
        {
            return this.ErrorProblem(errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Apply rules
    /// </summary>
    /// <remarks>
    /// Applies a list of automation rules, returning any validation errors found.
    /// This will apply the rules to the system if they are valid
    /// </remarks>
    [HttpPost("apply")]
    [ProducesSuccessNoContent]
    [ProducesAutomationValidationError]
    [ProducesUnauthorized]
    public async Task<IActionResult> Apply(
        [FromBody] IReadOnlyList<AutomationDto> rules,
        CancellationToken ct
    )
    {
        var duplicateIds = rules.GroupBy(r => r.Id).Where(g => g.Count() > 1);
        if (duplicateIds.Any())
        {
            return this.DuplicateProblem(duplicateIds.SelectMany(s => s));
        }

        var errors = await _service.ApplyAsync(rules, ct);
        if (errors.Count > 0)
        {
            return this.ErrorProblem(errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Get current rules
    /// </summary>
    /// <remarks>
    /// Returns the current automation rules applied to the system
    /// </remarks>
    [HttpGet]
    [ProducesSuccess]
    public async Task<IEnumerable<AutomationDto>> GetCurrent(CancellationToken ct) =>
        throw new NotImplementedException();
}
