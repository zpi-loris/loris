using Loris.Models;

namespace Loris.Messages.Database;

public record PingWriteMessage : IDbWriteMessage
{
    public required DevEui DevEui { get; init; }
    public required DateTime DateTime { get; init; }
}
