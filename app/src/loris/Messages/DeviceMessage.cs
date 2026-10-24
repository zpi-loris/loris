using Protocol.V1;

namespace app.Messages;

public record DeviceMessage
{
    public DateTime DateTime { get; init; }
    public required Message Payload { get; init; }
    public required byte[] DevEUI
    {
        get;
        init
        {
            field =
                (value.Length == 8)
                    ? value
                    : throw new ArgumentException("DevEUI must be 8 bytes long", nameof(value));
        }
    }
}
