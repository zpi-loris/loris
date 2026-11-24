using Loris.Models;

namespace Loris.Logging;

public static partial class DbWriteChannelLogging
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "Device {DevEui} already exists, bumped last seen to {DateTime}"
    )]
    public static partial void DeviceAlreadyExists(
        this ILogger logger,
        DevEui devEui,
        DateTime datetime
    );

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Received ping from device {DevEui} at {DateTime}"
    )]
    public static partial void ReceivedPing(this ILogger logger, DevEui devEui, DateTime datetime);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Device {DevEui} not found, dropping report at {DateTime}"
    )]
    public static partial void DeviceNotFound(
        this ILogger logger,
        DevEui devEui,
        DateTime datetime
    );

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "New device {DevEui} added with {SensorCount} sensors and {FunctionCount} functions"
    )]
    public static partial void NewDeviceAdded(
        this ILogger logger,
        DevEui devEui,
        int sensorCount,
        int functionCount
    );

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "Device {DevEui} has been in {Status} status but it's now pending"
    )]
    public static partial void ForcedToPending(
        this ILogger logger,
        DevEui devEui,
        DeviceStatus Status
    );
}
