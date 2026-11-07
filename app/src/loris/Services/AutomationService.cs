using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Models;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Messages;
using Loris.Types;

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

    public async Task<IReadOnlyList<AutomationError>> ValidateAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<AutomationError>();

        var validationResults = await ValidateRulesAsync(rules, cancellationToken);

        return rules
            .Zip(validationResults)
            .SelectMany(pair => ExtractRuleErrors(pair.First, pair.Second))
            .ToList();
    }

    private async Task<IEnumerable<AutomationValidationResult>> ValidateRulesAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken
    )
    {
        var triggersTask = ProcessRulesAsync(
            rules,
            r => r.Triggers,
            ValidateTrigger,
            cancellationToken
        );
        var conditionsTask = ProcessRulesAsync(
            rules,
            r => r.Conditions,
            ValidateCondition,
            cancellationToken
        );
        var actionsTask = ProcessRulesAsync(
            rules,
            r => r.Actions,
            ValidateAction,
            cancellationToken
        );

        await Task.WhenAll(triggersTask, conditionsTask, actionsTask);

        return Enumerable
            .Zip(await triggersTask, await conditionsTask, await actionsTask)
            .Select(v => new AutomationValidationResult(v.First, v.Second, v.Third));
    }

    private static IEnumerable<AutomationError> ExtractRuleErrors(
        AutomationDto rule,
        AutomationValidationResult results
    )
    {
        return ExtractBlockErrors("triggers", rule, rule.Triggers, results.Triggers)
            .Concat(ExtractBlockErrors("conditions", rule, rule.Conditions, results.Conditions))
            .Concat(ExtractBlockErrors("actions", rule, rule.Actions, results.Actions));
    }

    private static IEnumerable<AutomationError> ExtractBlockErrors<TBlock>(
        string sectionName,
        AutomationDto rule,
        IReadOnlyList<TBlock> blocks,
        IReadOnlyList<ValidationResult> results
    )
        where TBlock : IAutomationBlock
    {
        var ruleId = rule.Id.ToString();
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            if (!result.IsFailure)
                continue;

            var ServiceKey = blocks[i].ServiceKey;
            var prefix = $"{sectionName}.{i}.{ServiceKey}";

            foreach (var error in result.Errors)
            {
                yield return new(
                    ruleId,
                    rule.Name,
                    $"{prefix}.{error.Field}",
                    error.Value,
                    error.Description
                );
            }
        }
    }

    public IReadOnlyList<Automation> CompileAutomations(
        IReadOnlyList<AutomationDto> rules,
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
        try { }
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

    public Task SaveAutomationRulesToDiskAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    private async Task<(IEvent Event, Func<IEventMessage, bool> Filter)> CompileTrigger(
        ITriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<ITriggerCompiler>(dto.ServiceKey);
        return await Compile(dto, service!, cancellationToken);
    }

    private async Task<Func<StateCache, bool>> CompileCondition(
        IConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        return await Compile(dto, service!, cancellationToken);
    }

    private async Task<Action> CompileAction(IActionDto dto, CancellationToken cancellationToken)
    {
        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        return await Compile(dto, service!, cancellationToken);
    }

    // TODO: ===========================

    private async Task<ValidationResult> ValidateTrigger(
        ITriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<ITriggerCompiler>(dto.ServiceKey);
        return await service!.ValidateAsync(dto, cancellationToken);
    }

    private async Task<ValidationResult> ValidateCondition(
        IConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        return await service!.ValidateAsync(dto, cancellationToken);
    }

    private async Task<ValidationResult> ValidateAction(
        IActionDto dto,
        CancellationToken cancellationToken
    )
    {
        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        return await service!.ValidateAsync(dto, cancellationToken);
    }

    // TODO: ===========================

    private static async Task<TResult> Compile<TDto, TResult>(
        TDto dto,
        IAutomationBlockCompiler<TDto, TResult> service,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
        // if (await service.TryValidateAsync(dto, out var lookup, cancellationToken))
        // {
        //     return service.Compile(dto, lookup);
        // }
        // else
        // {
        //     throw new ArgumentException("Nie udało się, lipa");
        // }
    }

    private static async Task<List<List<TResult>>> ProcessRulesAsync<TItem, TResult>(
        IEnumerable<AutomationDto> rules,
        Func<AutomationDto, IEnumerable<TItem>> blockSelector,
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

    private sealed record AutomationValidationResult(
        IReadOnlyList<ValidationResult> Triggers,
        IReadOnlyList<ValidationResult> Conditions,
        IReadOnlyList<ValidationResult> Actions
    ) { }
}
