using Loris.Events;

namespace Loris.Messages.Event;

public record TimeEventMessage(IEvent Event) : IEventMessage { }
