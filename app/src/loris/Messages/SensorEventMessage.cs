using Loris.Events;
using Loris.Models;

namespace Loris.Messages;

public readonly record struct SensorEventMessage : IEventMessage
{
    public IEvent Event { get; init; }

    public required Report Previous { get; init; }
    public required Report Current { get; init; }
}
