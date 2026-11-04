using Chirpstack.Gateway;
using Loris.Events;

namespace Loris.Messages;

public record TestEventMessage : IEventMessage
{
    public required IEvent Event { get; init; }

    public static IEventMessage Default => new TestEventMessage { Event = TestEvent.Default };
}
