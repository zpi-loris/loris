using System.Collections.ObjectModel;
using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Actions.Test;
using Loris.Automations.Conditions;
using Loris.Automations.Conditions.Test;
using Loris.Automations.Triggers;
using Loris.Automations.Triggers.Test;
using Loris.Events;
using Loris.Messages;
using Loris.Protocol;
using Loris.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AutomationRulesController(IServiceProvider provider) : ControllerBase
{
    private readonly IServiceProvider _provider = provider;

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

    // POST api/<AutomationRulesController>
    [HttpPost]
    public void Post([FromBody] IReadOnlyList<AutomationRuleDto> rules)
    {
        // TODO: Separate as a service, creating list of compiled automations, asynchronously,
        // with result either success or exception to wrap for response output
        //
        // TODO: After successful parsing -> validating -> compiling call another service
        // to write pure json to file on server. This will be a basis for post-startup rules
        // compiling from server-source-of-truth
        //
        // TODO: each list of blocks (triggers, conditions, actions), can be parsed on seperate threads
        // leveraging fan-out architecture.
        // Considering:
        // - flattening each of rules block (triggers for all, conditions for all, etc...)
        // - bulk validation and compilation
        // - recreating sets, afterwards
        //
        // TODO: consider adding a way for both controller and service to:
        // - validate pure syntax (at the handler method level it's already good)
        // - validate syntax with device/sensor/function reference BUT DON'T SAVE AND DON'T COMPILE
        // - validate all, compile and save
        // This way user can have more control from the cli
        //
        foreach (var rule in rules)
        {
            IEvent? anEvent = default;
            Func<IEventMessage, bool>? filter = default;
            Func<ReadOnlyDictionary<DevEui, Report>, bool>? condition = default;
            Action? action = default;

            foreach (var trigger in rule.Triggers)
            {
                var triggerService = _provider.GetKeyedService<ITriggerService>(trigger.ServiceKey);
                if (triggerService!.TryValidate(trigger, out var lookup))
                {
                    (anEvent, filter) = triggerService!.CompileTrigger(trigger, lookup);
                }
            }

            foreach (var conditionRule in rule.Conditions)
            {
                var conditionService = _provider.GetKeyedService<IConditionService>(
                    conditionRule.ServiceKey
                );
                if (conditionService!.TryValidate(conditionRule, out var lookup))
                {
                    condition = conditionService!.CompileCondition(conditionRule, lookup);
                }
            }

            foreach (var actionRule in rule.Actions)
            {
                var actionService = _provider.GetKeyedService<IActionService>(
                    actionRule.ServiceKey
                );
                if (actionService!.TryValidate(actionRule, out var lookup))
                {
                    action = actionService!.CompileAction(actionRule, lookup);
                }
            }

            if (filter?.Invoke(TestEventMessage.Default) ?? false)
            {
                ReadOnlyDictionary<DevEui, Report> dict = new Dictionary<
                    DevEui,
                    Report
                >().AsReadOnly();
                if (condition?.Invoke(dict) ?? false)
                {
                    action?.Invoke();
                }
            }
        }
    }

    // // PUT api/<AutomationRulesController>/5
    // []
    // public void Put(int id, [FromBody] string value) { }
    //
    // // DELETE api/<AutomationRulesController>/5
    // [HttpDelete("{id}")]
    // public void Delete(int id) { }
}
