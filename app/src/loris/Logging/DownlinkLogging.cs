using Loris.Types;

namespace Loris.Logging;

public static partial class DownlinkLogging
{
    [LoggerMessage(
        EventId = 5401,
        EventName = nameof(DownlinkFailed),
        Level = LogLevel.Warning,
        Message = "Failed to send downlink to device {DevEui}: {Message}"
    )]
    public static partial void DownlinkFailed(this ILogger logger, DevEui devEui, string message);

    [LoggerMessage(
        EventId = 5402,
        EventName = nameof(DownlinkTimeout),
        Level = LogLevel.Warning,
        Message = "Timeout when sending downlink to device {DevEui}: {Message}"
    )]
    public static partial void DownlinkTimeout(this ILogger logger, DevEui devEui, string message);
}
