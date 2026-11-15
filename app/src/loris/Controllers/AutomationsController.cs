using Loris.Automations;
using Loris.Automations.Models;
using Loris.Extensions;
using Loris.Results;
using Loris.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType<AutomationProblemDetails>(StatusCodes.Status400BadRequest)]
public class AutomationsController(IAutomationService service) : ControllerBase
{
    private readonly IAutomationService _service = service;

    [HttpPost]
    public async Task<IActionResult> Validate(
        [FromBody] IReadOnlyList<AutomationDto> rules,
        CancellationToken ct
    )
    {
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
        var errors = await _service.ApplyAsync(rules, ct);
        if (errors.Count > 0)
        {
            return this.ErrorProblem(errors);
        }
        return Ok();
    }
}
