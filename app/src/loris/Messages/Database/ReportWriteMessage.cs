using Loris.Models;
using Loris.Protocol;

namespace Loris.Messages.Database;

public record ReportWriteMessage : IDbWriteMessage
{
    public required Guid SensorId { get; init; }
    public required Report Report { get; init; }
    public required DateTime DateTime { get; init; }
    public required DevEui DevEui { get; init; }
}
