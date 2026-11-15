using Loris.Types;

namespace Loris.Logging;

public static partial class DeviceChannelLogging
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        EventName = nameof(DeviceNotAccepted),
        Message = "Device {DevEui} was not accepted before, but sends  {MessageType}"
    )]
    public static partial void DeviceNotAccepted(
        this ILogger logger,
        DevEui devEui,
        string messageType
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        EventName = nameof(ReportReceived),
        Message = "Received report from device {DevEui} at {DateTime}"
    )]
    public static partial void ReportReceived(
        this ILogger logger,
        DevEui devEui,
        DateTime dateTime
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        EventName = nameof(DeviceAlreadyAccepted),
        Message = "Device {DevEui} was already accepted before, but sends capabilities"
    )]
    public static partial void DeviceAlreadyAccepted(this ILogger logger, DevEui devEui);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        EventName = nameof(PingReceived),
        Message = "Received ping from device {DevEui} at {DateTime}"
    )]
    public static partial void PingReceived(this ILogger logger, DevEui devEui, DateTime dateTime);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        EventName = nameof(CapabilitiesReceived),
        Message = "Received capabilities from device {DevEui} at {DateTime}"
    )]
    public static partial void CapabilitiesReceived(
        this ILogger logger,
        DevEui devEui,
        DateTime dateTime
    );
}
