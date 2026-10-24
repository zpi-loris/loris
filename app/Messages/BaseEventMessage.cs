using app.Events;

namespace app.Messages
{
    public abstract record BaseEventMessage
    {
        public required IEvent Event { get; init; }
    }
}
