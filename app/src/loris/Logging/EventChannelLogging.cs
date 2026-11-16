using Loris.Messages.Event;

namespace Loris.Logging;

public static partial class EventChannelLogging
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "New event received: {EventMessage}"
    )]
    public static partial void EventReceived(this ILogger logger, IEventMessage Eventmessage);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Automation {Id} {Name} triggered by event: {EventMessage}"
    )]
    public static partial void AutomationTriggered(
        this ILogger logger,
        Guid id,
        string name,
        IEventMessage Eventmessage
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Automation {Id} {Name} conditions met"
    )]
    public static partial void AutomationConditionsMet(this ILogger logger, Guid id, string name);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Automation {Id} {Name} conditions not met"
    )]
    public static partial void AutomationConditionsNotMet(
        this ILogger logger,
        Guid id,
        string name
    );
}
