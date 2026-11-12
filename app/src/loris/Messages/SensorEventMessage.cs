using Loris.Events;
using Loris.Protocol;

namespace Loris.Messages;

public record SensorEventMessage : IEventMessage
{
    public required IEvent Event { get; init; }
    public required Report? Previous { get; init; }
    public required Report Current { get; init; }
}
