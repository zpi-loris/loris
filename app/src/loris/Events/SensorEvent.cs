namespace Loris.Events;

/// <summary>
/// SensorEvent is an event representing a specific sensor
/// identified by its unique SensorId.
/// </summary>
public readonly record struct SensorEvent(Guid SensorId) : IEvent<SensorEvent> { }
