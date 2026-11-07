using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Messages;
using Loris.Protocol;
using Loris.Services;
using Loris.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

// TODO: consider adding a way for both controller and service to:
// - validate pure syntax (at the handler method level it's already good)
// - validate syntax with device/sensor/function reference BUT DON'T SAVE AND DON'T COMPILE
// - validate all, compile and save
// This way user can have more control from the cli

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
    [HttpPost]
    public async Task TestNoCancell([FromBody] IReadOnlyList<AutomationRuleDto> rules)
    {
        var automations = await _service.CompileAutomationsAsync(rules);
    }

    [HttpPost]
    public async Task Test([FromBody] IReadOnlyList<AutomationRuleDto> rules, CancellationToken ct)
    {
        var automations = await _service.CompileAutomationsAsync(rules, ct);
    }

    // // PUT api/<AutomationRulesController>/5
    // [HttpPut]
    // public void Put(int id, [FromBody] string value) { }
    //
    // // DELETE api/<AutomationRulesController>/5
    // [HttpDelete("{id}")]
    // public void Delete(int id) { }
}
