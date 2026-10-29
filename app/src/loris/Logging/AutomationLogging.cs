namespace Loris.Logging;

public static partial class AutomationLogging
{
    [LoggerMessage(
        EventId = 2001,
        EventName = nameof(EventMessageReceived),
        Level = LogLevel.Debug,
        Message = ""
    )]
    public static partial void EventMessageReceived(this ILogger logger);
}
