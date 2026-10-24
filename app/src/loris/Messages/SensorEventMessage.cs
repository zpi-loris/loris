using Protocol.V1;

namespace app.Messages;

public record SensorEventMessage : BaseEventMessage
{
    public required Report Previous { get; init; }
    public required Report Current { get; init; }
}
