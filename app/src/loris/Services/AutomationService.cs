using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Messages;

namespace Loris.Services;

public class AutomationService(ILogger<AutomationService> logger, IServiceProvider provider)
    : IAutomationService
{
    private readonly IServiceProvider _provider = provider;
    private readonly ILogger<AutomationService> _logger = logger;

    public void ApplyAutomations(IReadOnlyList<Automation> automations)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Automation>> CompileAutomationsAsync(
        IReadOnlyList<AutomationRuleDto> rules
    )
    {
        var triggersTask = ProcessRulesAsync(rules, r => r.Triggers, CompileTrigger);
        var conditionsTask = ProcessRulesAsync(rules, r => r.Conditions, CompileCondition);
        var actionsTask = ProcessRulesAsync(rules, r => r.Actions, CompileAction);

        var result = Task.WhenAll(triggersTask, conditionsTask, actionsTask);
        try
        {
            await result;
        }
        catch
        {
            foreach (var exception in result.Exception!.InnerExceptions)
            {
                Console.WriteLine($"operation failed, {exception}");
            }
        }

        // TODO: continue onward, gather errors, return them
        var triggers = await triggersTask;
        var conditions = await conditionsTask;
        var actions = await actionsTask;

        throw new NotImplementedException();
    }

    private async Task<(IEvent Event, Func<IEventMessage, bool> Filter)> CompileTrigger(
        ITriggerDto dto
    )
    {
        var service = _provider.GetKeyedService<ITriggerService>(dto.ServiceKey);
        // TODO: check if null supression is needed
        return await Compile(dto, service!);
    }

    private async Task<Func<StateCache, bool>> CompileCondition(IConditionDto dto)
    {
        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        // TODO: check if null supression is needed
        return await Compile(dto, service!);
    }

    private async Task<Action> CompileAction(IActionDto dto)
    {
        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        // TODO: check if null supression is needed
        return await Compile(dto, service!);
    }

    public Task SaveAutomationRulesToDiskAsync(IReadOnlyList<AutomationRuleDto> rules)
    {
        throw new NotImplementedException();
    }

    public Task ValidateAutomationsAsync(IReadOnlyList<AutomationRuleDto> rules)
    {
        throw new NotImplementedException();
    }

    private static async Task<TResult> Compile<TDto, TResult>(
        TDto dto,
        IAutomationBlockCompiler<TDto, TResult> service
    )
    {
        if (await service.TryValidateAsync(dto, out var lookup))
        {
            return service.Compile(dto, lookup);
        }
        else
        {
            throw new ArgumentException("Nie udało się, lipa");
        }
    }

    private static async Task<List<List<TResult>>> ProcessRulesAsync<TItem, TResult>(
        IEnumerable<AutomationRuleDto> rules,
        Func<AutomationRuleDto, IEnumerable<TItem>> blockSelector,
        Func<TItem, Task<TResult>> process
    )
    {
        var ruleTasks = rules.Select(async rule =>
        {
            var blockTasks = blockSelector(rule).Select(v => process(v));
            var results = await Task.WhenAll(blockTasks);
            return results.ToList();
        });

        var processed = await Task.WhenAll(ruleTasks);
        return [.. processed];
    }
}
