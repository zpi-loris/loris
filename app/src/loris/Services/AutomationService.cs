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

public sealed class AutomationService(
    ILogger<AutomationService> logger,
    IServiceProvider provider,
    AutomationIndex index
) : IAutomationService
{
    private readonly AutomationIndex _index = index;
    private readonly IServiceProvider _provider = provider;
    private readonly ILogger<AutomationService> _logger = logger;

    public async Task<IReadOnlyList<AutomationError>> ValidateAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        var validationResults = await ValidateRulesAsync(rules, cancellationToken);

        return rules
            .Zip(validationResults)
            .SelectMany(pair => ExtractRuleErrors(pair.First, pair.Second))
            .ToList();
    }

    public async Task<IReadOnlyList<AutomationError>> ApplyAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        var validationResults = await ValidateRulesAsync(rules, cancellationToken);
        var errors = rules
            .Zip(validationResults)
            .SelectMany(pair => ExtractRuleErrors(pair.First, pair.Second))
            .ToList();

        if (errors.Count > 0)
            return errors;

        List<Automation> automations = [];
        foreach (var (rule, validation) in rules.Zip(validationResults))
        {
            var triggers = rule
                .Triggers.Zip(validation.Triggers)
                .Select(pair => CompileTrigger(pair.First, pair.Second.Lookup));
            var conditions = rule
                .Conditions.Zip(validation.Conditions)
                .Select(pair => CompileCondition(pair.First, pair.Second.Lookup))
                .ToList();
            var actions = rule
                .Actions.Zip(validation.Actions)
                .Select(pair => CompileAction(pair.First, pair.Second.Lookup))
                .ToList();

            automations.AddRange(
                triggers.Select(trigger => new Automation(
                    rule.Id,
                    rule.Name,
                    trigger.Event,
                    trigger.Filter,
                    conditions,
                    actions
                ))
            );
        }

        _index.Update(automations.ToDictionary(v => v.Trigger));

        return [];
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

    public Task SaveToDiskAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    // ========================================================================
    // COMPILING SERVICE HELPERS
    // ========================================================================

    private (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger(
        ITriggerDto dto,
        LookupContext? lookup
    )
    {
        var service = _provider.GetKeyedService<ITriggerCompiler>(dto.ServiceKey);
        return service!.Compile(dto, lookup);
    }

    private Func<StateCache, bool> CompileCondition(IConditionDto dto, LookupContext? lookup)
    {
        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        return service!.Compile(dto, lookup);
    }

    private Action CompileAction(IActionDto dto, LookupContext? lookup)
    {
        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        return service!.Compile(dto, lookup);
    }

    // ========================================================================
    // VALIDATION SERVICE HELPERS
    // ========================================================================

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
