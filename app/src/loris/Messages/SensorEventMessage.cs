using Loris.Events;
using Loris.Protocol;

namespace Loris.Messages;

public readonly record struct SensorEventMessage : IEventMessage
{
    public required IEvent Event { get; init; }
    public required Report Previous { get; init; }
    public required Report Current { get; init; }
}
