using Loris.Models;
using Loris.Protocol;

namespace Loris.Messages.Database;

public record CapabilitiesWriteMessage : IDbWriteMessage
{
    public required Capabilities Capabilities { get; init; }
    public required DevEui DevEui { get; init; }
    public required DateTime DateTime { get; init; }
}
