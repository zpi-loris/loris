using Loris.Protocol;
using Loris.Types;

namespace Loris.Messages.Database;

public record SensorWriteMessage : IDbWriteMessage
{
    public required Report Report { get; init; }
    public required DevEui DevEui { get; init; }
    public required DateTime DateTime { get; init; }
}
