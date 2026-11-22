namespace Loris.Events;

public readonly record struct SensorEvent : IEvent<SensorEvent>
{
    /// <summary>
    /// Sensor primary key
    /// </summary>
    public required Guid SensorId { get; init; }
}
