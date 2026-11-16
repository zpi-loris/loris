using Loris.Automations;
using Loris.Extensions;
using Loris.Results;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[ProducesResponseType(StatusCodes.Status200OK, Description = "Success")]
[ProducesResponseType<AutomationProblemDetails>(
    StatusCodes.Status400BadRequest,
    Description = "Validation errors"
)]
public class AutomationsController(IAutomationService service) : ControllerBase
{
    private readonly IAutomationService _service = service;

    [HttpPost]
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

        return Ok();
    }

    [HttpPost]
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

        return Ok();
    }
}
