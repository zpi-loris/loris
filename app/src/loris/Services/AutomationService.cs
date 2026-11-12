using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Models;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Logging;
using Loris.Messages;

namespace Loris.Services;

public sealed class AutomationService(
    ILogger<AutomationService> logger,
    IServiceProvider provider,
    IAutomationIndex index
) : IAutomationService
{
    private readonly IAutomationIndex _index = index;
    private readonly IServiceProvider _provider = provider;
    private readonly ILogger<AutomationService> _logger = logger;

    public async Task<IReadOnlyList<AutomationError>> ValidateAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        var validationResults = await ValidateRulesAsync(rules, cancellationToken);

        var errors = rules
            .Zip(validationResults)
            .SelectMany(pair => ExtractRuleErrors(pair.First, pair.Second))
            .ToList();

        if (errors.Count == 0)
        {
            _logger.RulesValidationCompleted(rules.Count);
        }
        return errors;
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
                .Select(pair => CompileTrigger(rule, pair.First, pair.Second.Lookup));
            var conditions = rule
                .Conditions.Zip(validation.Conditions)
                .Select(pair => CompileCondition(rule, pair.First, pair.Second.Lookup))
                .ToList();
            var actions = rule
                .Actions.Zip(validation.Actions)
                .Select(pair => CompileAction(rule, pair.First, pair.Second.Lookup))
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

        Dictionary<IEvent, List<Automation>> index = [];
        foreach (var val in automations)
        {
            List<Automation>? list;
            if (!index.TryGetValue(val.Trigger, out list))
            {
                list = [];
                index.Add(val.Trigger, list);
            }
            list.Add(val);

            _logger.TriggerIndexed(val.Name, val.Id, val.Trigger.GetType().Name);
        }
        _index.Update(index);

        _logger.AutomationsApplied(rules.Count, automations.Count);

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

    private List<AutomationError> ExtractRuleErrors(
        AutomationDto rule,
        AutomationValidationResult results
    )
    {
        var ruleErrors = ExtractBlockErrors("triggers", rule, rule.Triggers, results.Triggers)
            .Concat(ExtractBlockErrors("conditions", rule, rule.Conditions, results.Conditions))
            .Concat(ExtractBlockErrors("actions", rule, rule.Actions, results.Actions))
            .ToList();

        if (ruleErrors.Count > 0)
        {
            _logger.RuleValidationFailed(
                rule.Name,
                rule.Id,
                ruleErrors.Count,
                ruleErrors[0].Pointer,
                ruleErrors[0].Details
            );
        }

        return ruleErrors;
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
        AutomationDto rule,
        ITriggerDto dto,
        LookupContext? lookup
    )
    {
        _logger.CompilingBlock("Trigger", dto.ServiceKey, rule.Name, rule.Id);

        var service = _provider.GetKeyedService<ITriggerCompiler>(dto.ServiceKey);
        if (service is null)
        {
            _logger.CompilerNotFound(nameof(ITriggerCompiler), dto.ServiceKey, rule.Name, rule.Id);
            throw new InvalidOperationException(
                $"No trigger compiler found for '{dto.ServiceKey}'"
            );
        }

        try
        {
            return service.Compile(dto, lookup);
        }
        catch (Exception ex)
        {
            _logger.BlockCompilationFailed(ex, "Trigger", dto.ServiceKey, rule.Name, rule.Id);
            throw;
        }
    }

    private Func<IStateCache, bool> CompileCondition(
        AutomationDto rule,
        IConditionDto dto,
        LookupContext? lookup
    )
    {
        _logger.CompilingBlock("Condition", dto.ServiceKey, rule.Name, rule.Id);

        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        if (service is null)
        {
            _logger.CompilerNotFound(
                nameof(IConditionCompiler),
                dto.ServiceKey,
                rule.Name,
                rule.Id
            );
            throw new InvalidOperationException(
                $"No condition compiler found for '{dto.ServiceKey}'"
            );
        }

        try
        {
            return service.Compile(dto, lookup);
        }
        catch (Exception ex)
        {
            _logger.BlockCompilationFailed(ex, "Condition", dto.ServiceKey, rule.Name, rule.Id);
            throw;
        }
    }

    private Action CompileAction(AutomationDto rule, IActionDto dto, LookupContext? lookup)
    {
        _logger.CompilingBlock("Action", dto.ServiceKey, rule.Name, rule.Id);

        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        if (service is null)
        {
            _logger.CompilerNotFound(nameof(IActionCompiler), dto.ServiceKey, rule.Name, rule.Id);
            throw new InvalidOperationException($"No action compiler found for '{dto.ServiceKey}'");
        }

        try
        {
            return service.Compile(dto, lookup);
        }
        catch (Exception ex)
        {
            _logger.BlockCompilationFailed(ex, "Action", dto.ServiceKey, rule.Name, rule.Id);
            throw;
        }
    }

    // ========================================================================
    // VALIDATION SERVICE HELPERS
    // ========================================================================

    private async Task<ValidationResult> ValidateTrigger(
        AutomationDto rule,
        ITriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        _logger.ValidatingBlock("Trigger", dto.ServiceKey, rule.Name, rule.Id);
        var service = _provider.GetKeyedService<ITriggerCompiler>(dto.ServiceKey);
        if (service is null)
        {
            _logger.CompilerNotFound(nameof(ITriggerCompiler), dto.ServiceKey, rule.Name, rule.Id);
            throw new InvalidOperationException(
                $"No trigger compiler found for '{dto.ServiceKey}'"
            );
        }
        try
        {
            return await service.ValidateAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.BlockValidationThrew(ex, dto.ServiceKey, rule.Name, rule.Id);
            throw;
        }
    }

    private async Task<ValidationResult> ValidateCondition(
        AutomationDto rule,
        IConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        _logger.ValidatingBlock("Condition", dto.ServiceKey, rule.Name, rule.Id);
        var service = _provider.GetKeyedService<IConditionCompiler>(dto.ServiceKey);
        if (service is null)
        {
            _logger.CompilerNotFound(
                nameof(IConditionCompiler),
                dto.ServiceKey,
                rule.Name,
                rule.Id
            );
            throw new InvalidOperationException(
                $"No condition compiler found for '{dto.ServiceKey}'"
            );
        }
        try
        {
            return await service.ValidateAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.BlockValidationThrew(ex, dto.ServiceKey, rule.Name, rule.Id);
            throw;
        }
    }

    private async Task<ValidationResult> ValidateAction(
        AutomationDto rule,
        IActionDto dto,
        CancellationToken cancellationToken
    )
    {
        _logger.ValidatingBlock("Action", dto.ServiceKey, rule.Name, rule.Id);
        var service = _provider.GetKeyedService<IActionCompiler>(dto.ServiceKey);
        if (service is null)
        {
            _logger.CompilerNotFound(nameof(IActionCompiler), dto.ServiceKey, rule.Name, rule.Id);
            throw new InvalidOperationException(
                $"No condition compiler found for '{dto.ServiceKey}'"
            );
        }
        try
        {
            return await service.ValidateAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.BlockValidationThrew(ex, dto.ServiceKey, rule.Name, rule.Id);
            throw;
        }
    }

    private static async Task<List<List<TResult>>> ProcessRulesAsync<TItem, TResult>(
        IEnumerable<AutomationDto> rules,
        Func<AutomationDto, IEnumerable<TItem>> blockSelector,
        Func<AutomationDto, TItem, CancellationToken, Task<TResult>> process,
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
                blockResults.Add(await process(rule, block, cancellationToken));
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
