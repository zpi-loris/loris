using app.Events;

namespace app.Messages;

/// <summary>
/// Represents an event notification payload
/// transmitted through the event processing channel.
/// </summary>
/// <remarks>
/// Additional metadata properties can be added
/// as these will be used for further automation filtering.
/// </remarks>
public interface IEventMessage
{
    /// <summary>
    /// Gets the event key used to resolve matching
    /// automation delegates from the inverted index.
    /// </summary>
    public IEvent Event { get; init; }
}
