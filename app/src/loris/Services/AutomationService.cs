using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Models;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Messages;

namespace Loris.Services;

public sealed class AutomationService(ILogger<AutomationService> logger, IServiceProvider provider)
    : IAutomationService
{
    private readonly IServiceProvider _provider = provider;
    private readonly ILogger<AutomationService> _logger = logger;

    public void ApplyAutomations(IReadOnlyList<Automation> automations)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Automation>> CompileAutomationsAsync(
        IReadOnlyList<AutomationRuleDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        var triggersTask = ProcessRulesAsync(
            rules,
            r => r.Triggers,
            CompileTrigger,
            cancellationToken
        );
        var conditionsTask = ProcessRulesAsync(
            rules,
            r => r.Conditions,
            CompileCondition,
            cancellationToken
        );
        var actionsTask = ProcessRulesAsync(
            rules,
            r => r.Actions,
            CompileAction,
            cancellationToken
        );

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
        // var triggers = await triggersTask;
        // var conditions = await conditionsTask;
        // var actions = await actionsTask;

        throw new NotImplementedException("End of implementation");
    }

    private async Task<(IEvent Event, Func<IEventMessage, bool> Filter)> CompileTrigger(
        ITriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<ITriggerService>(dto.ServiceKey);
        // TODO: check if null supression is needed
        return await Compile(dto, service!, cancellationToken);
    }

    private async Task<Func<StateCache, bool>> CompileCondition(
        IConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        // TODO: check if null supression is needed
        return await Compile(dto, service!, cancellationToken);
    }

    private async Task<Action> CompileAction(IActionDto dto, CancellationToken cancellationToken)
    {
        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        // TODO: check if null supression is needed
        return await Compile(dto, service!, cancellationToken);
    }

    public Task SaveAutomationRulesToDiskAsync(
        IReadOnlyList<AutomationRuleDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public Task ValidateAutomationsAsync(
        IReadOnlyList<AutomationRuleDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    private static async Task<TResult> Compile<TDto, TResult>(
        TDto dto,
        IAutomationBlockCompiler<TDto, TResult> service,
        CancellationToken cancellationToken
    )
    {
        if (await service.TryValidateAsync(dto, out var lookup, cancellationToken))
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
        Func<TItem, CancellationToken, Task<TResult>> process,
        CancellationToken cancellationToken
    )
    {
        var results = new List<List<TResult>>();
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var blockResults = new List<TResult>();
            foreach (var block in blockSelector(rule))
            {
                cancellationToken.ThrowIfCancellationRequested();
                blockResults.Add(await process(block, cancellationToken));
            }
            results.Add(blockResults);
        }
        return results;
    }
}
