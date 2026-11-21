using Loris.Automations;

namespace Loris.Logging;

public static partial class IngestionLogging
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "{JobName} started")]
    public static partial void JobStarted(this ILogger logger, string jobName);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "{JobName} finished")]
    public static partial void JobFinished(this ILogger logger, string jobName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Critical,
        Message = "{Count} Automations not applied due to errors: {Errors}"
    )]
    public static partial void AutomationErrors(
        this ILogger logger,
        int count,
        IEnumerable<AutomationError> errors
    );

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Critical,
        Message = "Automation {Id} failed deserialization"
    )]
    public static partial void AutomationDeserializationError(
        this ILogger logger,
        Exception ex,
        Guid id
    );
}
