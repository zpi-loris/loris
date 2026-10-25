using app.Protocol;
using app.Types;

namespace app.Messages;

public readonly record struct UplinkMessage : IChirpstackMessage
{
    public required DevEui DevEUI { get; init; }
    public required Uplink Payload { get; init; }
    public required DateTime DateTime { get; init; }
}
