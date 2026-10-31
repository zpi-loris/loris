namespace Loris.MQTT.Client;

public record ClientSettings
{
    public required string Host { get; init; }

    private readonly int? _port;
    public int Port
    {
        get
        {
            if (!_port.HasValue)
            {
                return 1883;
            }
            return _port.Value;
        }
        init { _port = value; }
    }
    public Guid? ApplicationId { get; init; }
}
