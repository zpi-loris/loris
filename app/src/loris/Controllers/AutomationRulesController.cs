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
        // for each rule there are:
        // - triggers
        // - conditions
        // - action defined
        //
        // each trigger implements ITriggerDto
        // each condition implements IConditionDto
        // each action implements IActionDto
        //
        // foreach rule in rules do
        //
        // validate triggers
        // validate conditions
        // validate actions
        //
        // Validation is purely database agnostic (DeviceName, sensorName, functionName)
        // maybe during validation we can ensure to gather:
        // - DevEUI for all of DeviceNames
        //   - sensor id for each device
        //   - function id for each device
        //
        // gather all of these in some sort of dictionary structure

        IEvent? anEvent = default;
        Func<IEventMessage, bool>? filter = default;
        Func<ReadOnlyDictionary<DevEui, Report>, bool>? condition = default;
        Action? action = default;
        foreach (var rule in rules)
        {
            foreach (var trigger in rule.Triggers)
            {
                var triggerService = _provider.GetKeyedService<ITriggerService>(trigger.ServiceKey);
                (anEvent, filter) = triggerService!.CompileTrigger(trigger);
            }
            foreach (var conditionRule in rule.Conditions)
            {
                var conditionService = _provider.GetKeyedService<IConditionService>(
                    conditionRule.ServiceKey
                );
                condition = conditionService!.CompileCondition(conditionRule);
            }
            foreach (var actionRule in rule.Actions)
            {
                var actionService = _provider.GetKeyedService<IActionService>(
                    actionRule.ServiceKey
                );
                action = actionService!.CompileAction(actionRule);
            }
        }

        if (filter?.Invoke(TestEventMessage.Default) ?? false)
        {
            ReadOnlyDictionary<DevEui, Report> dict = new Dictionary<DevEui, Report>().AsReadOnly();
            if (condition?.Invoke(dict) ?? false)
            {
                action?.Invoke();
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
