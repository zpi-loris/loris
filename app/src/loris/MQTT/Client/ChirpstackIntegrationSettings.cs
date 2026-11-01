namespace Loris.MQTT.Client;

public record ChirpstackIntegrationSettings
{
    public required string Host { get; init; }
    public int Port { get; init; } = 1883;
    public Guid? ApplicationId { get; init; }
    public int ReconnectInterval { get; init; } = 5;
}
