namespace Loris.Logging;

public static partial class EndpointsLogging
{
    [LoggerMessage(
        EventId = 5401,
        EventName = nameof(ReadingsDeleted),
        Level = LogLevel.Information,
        Message = "Deleted readings for sensor {SensorId}"
    )]
    public static partial void ReadingsDeleted(this ILogger logger, Guid sensorId);

    [LoggerMessage(
        EventId = 5123,
        EventName = nameof(SensorDeleted),
        Level = LogLevel.Information,
        Message = "Sensor {SensorId} was deleted"
    )]
    public static partial void SensorDeleted(this ILogger logger, Guid sensorId);

    [LoggerMessage(
        EventId = 5401,
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
        EventId = 5123,
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
