namespace Loris.Logging;

public static partial class EndpointsLogging
{
    [LoggerMessage(
        EventId = 7000,
        EventName = nameof(ReadingsDeleted),
        Level = LogLevel.Information,
        Message = "Deleted readings for sensor {SensorId}"
    )]
    public static partial void ReadingsDeleted(this ILogger logger, Guid sensorId);

    [LoggerMessage(
        EventId = 7001,
        EventName = nameof(SensorDeleted),
        Level = LogLevel.Information,
        Message = "Sensor {SensorId} was deleted"
    )]
    public static partial void SensorDeleted(this ILogger logger, Guid sensorId);

    [LoggerMessage(
        EventId = 7002,
        EventName = nameof(DeviceDeleted),
        Level = LogLevel.Information,
        Message = "Deleted {FriendlyName} device of with id {DeviceId}"
    )]
    public static partial void DeviceDeleted(
        this ILogger logger,
        string friendlyName,
        Guid deviceId
    );

    [LoggerMessage(
        EventId = 7003,
        EventName = nameof(DeviceRenamed),
        Level = LogLevel.Information,
        Message = "Device {DeviceId} was renamed from {OldName} to {NewName}"
    )]
    public static partial void DeviceRenamed(
        this ILogger logger,
        Guid deviceId,
        string oldName,
        string newName
    );
}
