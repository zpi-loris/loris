using System.Text.Json;
using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Events;
using Loris.Logging;
using Loris.Messages.Event;
using Loris.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Loris.Services;

public sealed class AutomationService(
    ILogger<AutomationService> logger,
    LorisDbContext context,
    IServiceProvider provider,
    IAutomationIndex index,
    IOptions<JsonOptions> jsonOptions
) : IAutomationService
{
    private readonly IAutomationIndex _index = index;
    private readonly IServiceProvider _provider = provider;
    private readonly LorisDbContext _ctx = context;
    private readonly ILogger<AutomationService> _logger = logger;
    private readonly IOptions<JsonOptions> _jsonOptions = jsonOptions;

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
        bool force = false,
        CancellationToken cancellationToken = default
    )
    {
        var validationResults = await ValidateRulesAsync(rules, cancellationToken);
        var errors = rules
            .Zip(validationResults)
            .SelectMany(ruleAndResult =>
                ExtractRuleErrors(ruleAndResult.First, ruleAndResult.Second)
            )
            .ToList();

        if (errors.Count > 0 && !force)
        {
            return errors;
        }
        else if (errors.Count > 0)
        {
            // force is true,
            // we want to apply valid rules only
            FilterOutErrors(ref rules, ref validationResults, errors);
        }

        // saving is implemented using a transaction. If a cancellation
        // is requested anytime during the method call, the rules are not replaced.
        // The dbContext should throw itself, but just to be sure I will throw (if cancelled)
        // again so the automations are not replaced in the runtime if the cancellation is requested
        await SaveToDiskAsync(rules, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // compile and apply rules
        var automations = Compile(rules, validationResults);
        ApplyToIndex(automations);

        _logger.AutomationsApplied(rules.Count, automations.Count);

        return [];
    }

    private static void FilterOutErrors(
        ref IReadOnlyList<AutomationDto> rules,
        ref List<AutomationValidationResult> validationResults,
        List<AutomationError> errors
    )
    {
        var invalidAutomations = errors.Select(e => e.Id).ToHashSet();

        var filteredRules = new List<AutomationDto>(rules.Count);
        var filteredResults = new List<AutomationValidationResult>(rules.Count);

        // filter out the rules that have errors if force is true
        foreach (var (rule, result) in rules.Zip(validationResults))
        {
            if (invalidAutomations.Contains(rule.Id))
            {
                continue;
            }

            filteredRules.Add(rule);
            filteredResults.Add(result);
        }

        rules = filteredRules;
        validationResults = filteredResults;
    }

    private void ApplyToIndex(List<CompiledAutomation> automations)
    {
        Dictionary<IEvent, List<CompiledAutomation>> index = [];
        foreach (var val in automations)
        {
            if (!index.TryGetValue(val.Trigger, out List<CompiledAutomation>? list))
            {
                list = [];
                index.Add(val.Trigger, list);
            }
            list.Add(val);

            _logger.TriggerIndexed(val.Name, val.Id, val.Trigger.GetType().Name);
        }
        _index.Update(index);
    }

    private List<CompiledAutomation> Compile(
        IReadOnlyList<AutomationDto> rules,
        IEnumerable<AutomationValidationResult> validationResults
    )
    {
        List<CompiledAutomation> automations = [];
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
                triggers.Select(trigger => new CompiledAutomation(
                    rule.Id,
                    rule.Name,
                    trigger.Event,
                    trigger.Filter,
                    conditions,
                    actions
                ))
            );
        }

        return automations;
    }

    private async Task<List<AutomationValidationResult>> ValidateRulesAsync(
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
            .Select(v => new AutomationValidationResult(v.First, v.Second, v.Third))
            .ToList();
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
        IReadOnlyList<BlockValidationResult> results
    )
        where TBlock : IAutomationBlock
    {
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
                    rule.Id,
                    rule.Name,
                    $"{prefix}.{error.Field}",
                    error.Value,
                    error.Description
                );
            }
        }
    }

    private async Task SaveToDiskAsync(
        IReadOnlyList<AutomationDto> rules,
        CancellationToken cancellationToken = default
    )
    {
        // use MVC defined jsonSerializerOptions. It is the same, that is applied to
        // the .UseControllers().AddJsonOptions(options => {})
        var jsonSerializerOptions = _jsonOptions.Value.JsonSerializerOptions;

        // we need to use transaction, to ensure automations replacement does
        // not corrupt data on failure

        using var transaction = await _ctx.Database.BeginTransactionAsync(cancellationToken);

        int deleted = 0;
        try
        {
            // delete all rows from the table
            deleted = await _ctx.Automations.ExecuteDeleteAsync(cancellationToken);

            // serialize and track all the new rules
            var automationModels = rules.Select(rule => new Automation
            {
                Name = rule.Name,
                AutomationId = rule.Id,
                Triggers = JsonSerializer.Serialize(rule.Triggers, jsonSerializerOptions),
                Conditions = JsonSerializer.Serialize(rule.Conditions, jsonSerializerOptions),
                Actions = JsonSerializer.Serialize(rule.Actions, jsonSerializerOptions),
            });
            _ctx.Automations.AddRange(automationModels);
            await _ctx.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // At this point, this is an internal server error, not client related
            _logger.LogError("There was an exception while replacing old rules: {ex}", ex.Message);
            throw;
        }

        _logger.LogInformation("{Num} rules removed from the database", deleted);
        _logger.LogInformation("{Num} new rules applied to the database", rules.Count);
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

    private Func<CancellationToken, Task> CompileAction(
        AutomationDto rule,
        IActionDto dto,
        LookupContext? lookup
    )
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

    private async Task<BlockValidationResult> ValidateTrigger(
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

    private async Task<BlockValidationResult> ValidateCondition(
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

    private async Task<BlockValidationResult> ValidateAction(
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
        IReadOnlyList<BlockValidationResult> Triggers,
        IReadOnlyList<BlockValidationResult> Conditions,
        IReadOnlyList<BlockValidationResult> Actions
    ) { }
}
