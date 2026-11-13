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

    // // GET: api/<AutomationRulesController>
    // [HttpGet]
    // public IEnumerable<string> Get()
    // {
    //     return new string[] { "value1", "value2" };
    // }
    //
    // // GET api/<AutomationRulesController>/5
    // [HttpGet("{id}")]
    // public string Get(int id)
    // {
    //     return "value";
    // }
    // // TODO: Test no cancell
    // [HttpPost]
    // public async Task TestNoCancell([FromBody] IReadOnlyList<AutomationDto> rules)
    // {
    //     var automations = await _service.CompileAutomationsAsync(rules);
    // }

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

    // // PUT api/<AutomationRulesController>/5
    // [HttpPut]
    // public void Put(int id, [FromBody] string value) { }
    //
    // // DELETE api/<AutomationRulesController>/5
    // [HttpDelete("{id}")]
    // public void Delete(int id) { }
}
