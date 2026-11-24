using Loris.Models;

namespace Loris.Logging;

public static partial class DownlinkLogging
{
    [LoggerMessage(
        EventId = 6001,
        EventName = nameof(DownlinkFailed),
        Level = LogLevel.Warning,
        Message = "Failed to send downlink to device {DevEui}: {Message}"
    )]
    public static partial void DownlinkFailed(this ILogger logger, DevEui devEui, string message);
}
