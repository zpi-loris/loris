namespace Loris.Logging;

public static partial class AutomationLogging
{
    [LoggerMessage(
        EventId = 5000,
        EventName = nameof(AutomationsApplied),
        Level = LogLevel.Information,
        Message = "Successfully applied {RuleCount} automation rules, generating {AutomationCount} triggered automations"
    )]
    public static partial void AutomationsApplied(
        this ILogger logger,
        int ruleCount,
        int automationCount
    );

    [LoggerMessage(
        EventId = 5001,
        EventName = nameof(RulesValidationCompleted),
        Level = LogLevel.Information,
        Message = "Validation passed for all {RuleCount} automation rules"
    )]
    public static partial void RulesValidationCompleted(this ILogger logger, int ruleCount);

    [LoggerMessage(
        EventId = 5002,
        EventName = nameof(ValidatingBlock),
        Level = LogLevel.Debug,
        Message = "Validating {Section} block [{ServiceKey}] for rule '{RuleName}' ({RuleId})"
    )]
    public static partial void ValidatingBlock(
        this ILogger logger,
        string section,
        string serviceKey,
        string ruleName,
        Guid ruleId
    );

    [LoggerMessage(
        EventId = 5003,
        EventName = nameof(CompilingBlock),
        Level = LogLevel.Debug,
        Message = "Compiling {Section} block [{ServiceKey}] for rule '{RuleName}' ({RuleId})"
    )]
    public static partial void CompilingBlock(
        this ILogger logger,
        string section,
        string serviceKey,
        string ruleName,
        Guid ruleId
    );

    [LoggerMessage(
        EventId = 5004,
        EventName = nameof(TriggerIndexed),
        Level = LogLevel.Debug,
        Message = "Indexed automation '{RuleName}' ({RuleId}) under trigger event {EventType}"
    )]
    public static partial void TriggerIndexed(
        this ILogger logger,
        string ruleName,
        Guid ruleId,
        string eventType
    );

    [LoggerMessage(
        EventId = 5005,
        EventName = nameof(RuleValidationFailed),
        Level = LogLevel.Warning,
        Message = "Validation failed for rule '{RuleName}' ({RuleId}) with {ErrorCount} errors. First error at '{Field}': {ErrorDescription}"
    )]
    public static partial void RuleValidationFailed(
        this ILogger logger,
        string ruleName,
        Guid ruleId,
        int errorCount,
        string field,
        string errorDescription
    );

    [LoggerMessage(
        EventId = 5006,
        EventName = nameof(CompilerNotFound),
        Level = LogLevel.Warning,
        Message = "No {CompilerType} registered with ServiceKey '{ServiceKey}' for rule '{RuleName}' ({RuleId})"
    )]
    public static partial void CompilerNotFound(
        this ILogger logger,
        string compilerType,
        string serviceKey,
        string ruleName,
        Guid ruleId
    );

    [LoggerMessage(
        EventId = 5007,
        EventName = nameof(BlockCompilationFailed),
        Level = LogLevel.Error,
        Message = "Unexpected failure compiling {Section} block [{ServiceKey}] for rule '{RuleName}' ({RuleId})"
    )]
    public static partial void BlockCompilationFailed(
        this ILogger logger,
        Exception ex,
        string section,
        string serviceKey,
        string ruleName,
        Guid ruleId
    );

    [LoggerMessage(
        EventId = 5008,
        EventName = nameof(BlockValidationThrew),
        Level = LogLevel.Error,
        Message = "Compiler {ServiceKey} threw an exception during validation of rule '{RuleName}' ({RuleId})"
    )]
    public static partial void BlockValidationThrew(
        this ILogger logger,
        Exception ex,
        string serviceKey,
        string ruleName,
        Guid ruleId
    );

    [LoggerMessage(
        EventId = 5009,
        EventName = nameof(RulesReplacementFailed),
        Level = LogLevel.Error,
        Message = "Unexpected failure while replacing old rules with new rules"
    )]
    public static partial void RulesReplacementFailed(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 5010,
        EventName = nameof(RulesReplacementSucceeded),
        Level = LogLevel.Information,
        Message = "Successfully replaced {Old} old rules with {New} new rules"
    )]
    public static partial void RulesReplacementSucceeded(this ILogger logger, int old, int @new);
}
