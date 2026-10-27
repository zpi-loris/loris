using Loris.Types;

namespace Loris.Events;

public readonly record struct SensorEvent : IEvent<SensorEvent>
{
    public required DevEui DevEui { get; init; }
    public required int SensorID { get; init; }
}
