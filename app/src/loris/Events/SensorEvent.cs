using app.Types;

namespace app.Events;

public readonly record struct SensorEvent : IEvent
{
    public required DevEui DevEui { get; init; }
    public required int SensorID { get; init; }
}
