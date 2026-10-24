using app.Types;
using Protocol.V1;

namespace app.Messages;

public readonly record struct UplinkMessage : IChirpstackMessage
{
    public required DevEui DevEUI { get; init; }
    public required Message Payload { get; init; }
    public required DateTime DateTime { get; init; }
}
