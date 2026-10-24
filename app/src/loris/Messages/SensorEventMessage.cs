using app.Events;
using Protocol.V1;

namespace app.Messages;

public readonly record struct SensorEventMessage : IEventMessage
{
    public IEvent Event { get; init; }

    public required Report Previous { get; init; }
    public required Report Current { get; init; }
}
