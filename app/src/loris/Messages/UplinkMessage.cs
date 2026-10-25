using Loris.Protocol;
using Loris.Types;

namespace Loris.Messages;

public readonly record struct UplinkMessage : IChirpstackMessage
{
    public required DevEui DevEUI { get; init; }
    public required Uplink Payload { get; init; }
    public required DateTime DateTime { get; init; }
}
