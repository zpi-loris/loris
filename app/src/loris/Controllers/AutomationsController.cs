using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Messages;
using Loris.Protocol;
using Loris.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AutomationsController(ILogger<AutomationsController> logger, IServiceProvider provider)
    : ControllerBase
{
    private readonly IServiceProvider _provider = provider;
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
    public async Task Test([FromBody] IReadOnlyList<AutomationRuleDto> rules)
    {
        var actionsTask = Task.Run(() =>
        {
            return rules.Select(rule =>
                rule.Actions.Select(async action => await NewMethod(action))
            );
        });

        var actions = (await actionsTask).SelectMany(g => g);

        foreach (var action in actions)
        {
            try
            {
                await action;
            }
            catch (ArgumentException e)
            {
                _logger.LogError("There was an error compiling the aciton: {Data}", e.Data);
            }
        }
    }

    [HttpPost]
    public async Task Apply([FromBody] IReadOnlyList<AutomationRuleDto> rules)
    {
        var triggersTask = Task.Run(() =>
        {
            return rules.Select(rule =>
                rule.Triggers.Select(async trigger =>
                {
                    var triggerService = _provider.GetKeyedService<ITriggerService>(
                        trigger.ServiceKey
                    );
                    if (await triggerService!.TryValidateAsync(trigger, out var lookup))
                    {
                        return triggerService!.Compile(trigger, lookup);
                    }
                    else
                    {
                        throw new ArgumentException("Nie udało się, lipa");
                    }
                })
            );
        });

        var conditionsTask = Task.Run(() =>
        {
            return rules.Select(rule =>
                rule.Conditions.Select(async condition =>
                {
                    var conditionservice = _provider.GetKeyedService<IConditionCompiler>(
                        condition.ServiceKey
                    );
                    if (await conditionservice!.TryValidateAsync(condition, out var lookup))
                    {
                        return conditionservice!.Compile(condition, lookup);
                    }
                    else
                    {
                        throw new ArgumentException("Nie udało się, lipa");
                    }
                })
            );
        });

        var actionsTask = Task.Run(() =>
        {
            return rules.Select(rule =>
                rule.Actions.Select(async action => await NewMethod(action))
            );
        });

        var task = Task.WhenAll(triggersTask, conditionsTask, actionsTask);
        try
        {
            await task;
        }
        catch
        {
            foreach (var exception in task.Exception!.InnerExceptions)
            {
                Console.WriteLine($"operation failed, {exception}");
            }
        }

        var triggers = await triggersTask;
        var conditions = await conditionsTask;
        var actions = await actionsTask;
    }

    private async Task<Action> NewMethod(IActionDto action)
    {
        var actionservice = _provider.GetKeyedService<IActionCompiler>(action.ServiceKey);
        if (await actionservice!.TryValidateAsync(action, out var lookup))
        {
            return actionservice!.Compile(action, lookup);
        }
        else
        {
            throw new ArgumentException("Nie udało się, lipa");
        }
    }

    // [HttpPost]
    // public void Post([FromBody] IReadOnlyList<AutomationRuleDto> rules)
    // {
    //     var order = Enumerable.Range(0, rules.Count());
    //
    //     var allTriggers = rules.SelectMany(rule => rule.Triggers);
    //     var allConditions = rules.SelectMany(rule => rule.Conditions);
    //     var allActions = rules.SelectMany(rule => rule.Actions);
    //
    //     var triggersTask = Task<IEnumerable<string>>.Run(() =>
    //     {
    //         List<string> strings = [];
    //
    //         for (int i = 0; i < 10; i++)
    //         {
    //             strings.Add($"value {i}");
    //         }
    //         return strings;
    //     });
    //
    //     foreach (var trigger in allTriggers)
    //     {
    //         var triggerService = _provider.GetKeyedService<ITriggerService>(trigger.ServiceKey);
    //         if (triggerService!.TryValidateAsync(trigger, out var lookup))
    //         {
    //             (anEvent, filter) = triggerService!.CompileTrigger(trigger, lookup);
    //         }
    //     }
    //
    //     foreach (var condition in allConditions)
    //     {
    //         Console.WriteLine(condition);
    //     }
    //
    //     foreach (var action in allActions)
    //     {
    //         Console.WriteLine(action);
    //     }
    //
    //     return;
    //
    //     // TODO: Separate as a service, creating list of compiled automations, asynchronously,
    //     // with result either success or exception to wrap for response output
    //     //
    //     // TODO: After successful parsing -> validating -> compiling call another service
    //     // to write pure json to file on server. This will be a basis for post-startup rules
    //     // compiling from server-source-of-truth
    //     //
    //     // TODO: each list of blocks (triggers, conditions, actions), can be parsed on seperate threads
    //     // leveraging fan-out architecture.
    //     // Considering:
    //     // - flattening each of rules block (triggers for all, conditions for all, etc...)
    //     // - bulk validation and compilation
    //     // - recreating sets, afterwards
    //     //
    //     // TODO: consider adding a way for both controller and service to:
    //     // - validate pure syntax (at the handler method level it's already good)
    //     // - validate syntax with device/sensor/function reference BUT DON'T SAVE AND DON'T COMPILE
    //     // - validate all, compile and save
    //     // This way user can have more control from the cli
    //     //
    //     foreach (var rule in rules)
    //     {
    //         IEvent? anEvent = default;
    //         Func<IEventMessage, bool>? filter = default;
    //         Func<StateCache, bool>? condition = default;
    //         Action? action = default;
    //
    //         foreach (var trigger in rule.Triggers)
    //         {
    //             var triggerService = _provider.GetKeyedService<ITriggerService>(trigger.ServiceKey);
    //             if (triggerService!.TryValidateAsync(trigger, out var lookup))
    //             {
    //                 (anEvent, filter) = triggerService!.CompileTrigger(trigger, lookup);
    //             }
    //         }
    //
    //         foreach (var conditionRule in rule.Conditions)
    //         {
    //             var conditionService = _provider.GetKeyedService<IConditionService>(
    //                 conditionRule.ServiceKey
    //             );
    //             if (conditionService!.TryValidateAsync(conditionRule, out var lookup))
    //             {
    //                 condition = conditionService!.CompileCondition(conditionRule, lookup);
    //             }
    //         }
    //
    //         foreach (var actionRule in rule.Actions)
    //         {
    //             var actionService = _provider.GetKeyedService<IActionService>(
    //                 actionRule.ServiceKey
    //             );
    //             if (actionService!.TryValidate(actionRule, out var lookup))
    //             {
    //                 action = actionService!.CompileAction(actionRule, lookup);
    //             }
    //         }
    //
    //         if (filter?.Invoke(TestEventMessage.Default) ?? false)
    //         {
    //             var state = new StateCache();
    //             if (condition?.Invoke(state) ?? false)
    //             {
    //                 action?.Invoke();
    //             }
    //         }
    //     }
    // }

    // IEvent? anEvent = default;
    // Func<IEventMessage, bool>? filter = default;
    // Func<ReadOnlyDictionary<DevEui, Report>, bool>? condition = default;
    // Action? action = default;
    // public Func<ReadOnlyDictionary<DevEui, Report>, bool>? Condition { get; init; }

    // // PUT api/<AutomationRulesController>/5
    // []
    // public void Put(int id, [FromBody] string value) { }
    //
    // // DELETE api/<AutomationRulesController>/5
    // [HttpDelete("{id}")]
    // public void Delete(int id) { }
}
