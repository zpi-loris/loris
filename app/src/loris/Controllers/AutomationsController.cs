using Loris.Automations;
using Loris.Automations.Models;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AutomationsController(
    ILogger<AutomationsController> logger,
    IAutomationService service
) : ControllerBase
{
    private readonly IAutomationService _service = service;
    private readonly ILogger<AutomationsController> _logger = logger;

    [HttpPost]
    public async Task<IActionResult> Validate(
        [FromBody] IReadOnlyList<AutomationDto> rules,
        CancellationToken ct
    )
    {
        var errors = await _service.ValidateAsync(rules, ct);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
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
            return ValidationProblem(errors);
        }
        return Ok();
    }

    private ObjectResult ValidationProblem(IReadOnlyList<AutomationError> errors)
    {
        var groupedErrors = errors
            .GroupBy(e => new { e.Id, e.Name })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                Errors = g.Select(v => new
                {
                    v.Pointer,
                    v.Value,
                    v.Details,
                }),
            });

        return Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            type: "/errors/automation-validation-failed",
            title: "One or more automation rules failed validation.",
            extensions: new Dictionary<string, object?> { ["invalidRules"] = groupedErrors }
        );
    }
}
