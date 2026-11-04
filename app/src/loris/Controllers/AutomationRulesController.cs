using Loris.Automations;
using Loris.Automations.Actions.Test;
using Loris.Automations.Conditions.Test;
using Loris.Automations.Triggers;
using Loris.Automations.Triggers.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AutomationRulesController : ControllerBase
{
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
        foreach (var rule in rules)
        {
            foreach (var trigger in rule.Triggers)
            {
                if (trigger is TestTriggerDto value)
                {
                    Console.WriteLine(value);
                }
            }
            foreach (var condition in rule.Conditions)
            {
                if (condition is TestConditionDto value)
                {
                    Console.WriteLine(value);
                }
            }
            foreach (var action in rule.Actions)
            {
                if (action is TestActionDto value)
                {
                    Console.WriteLine(value);
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
