using Loris.Protocol;
using Loris.Types;

namespace Loris.Messages.Database;

public record CapabilitiesWriteMessage : IDbWriteMessage
{
    public required Capabilities Capabilities { get; init; }
    public required DevEui DevEui { get; init; }
    public required DateTime DateTime { get; init; }
}
