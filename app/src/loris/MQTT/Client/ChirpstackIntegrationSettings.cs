namespace Loris.MQTT.Client;

public record ChirpstackIntegrationSettings
{
    /// <summary>
    /// Host ip address or domain name resolving to chirpstack<br/>
    /// MQTT integration broker.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// MQTT integration broker listening port.<br/>
    /// 1883 is default
    /// </summary>
    public int Port { get; init; } = 1883;

    /// <summary>
    /// <para>
    /// Application ID specifies the application made specifically<br/>
    /// for Loris in Chirpstack LoRaWAN server<br/>
    /// </para>
    /// <para>
    /// If the property is not specified, the client is substituting it with<br/>
    /// single level wildcard "+"
    /// </para>
    /// </summary>
    public Guid? ApplicationId { get; init; }

    /// <summary>
    /// <para>
    /// Specifies time in seconds, how frequently the client<br/>
    /// should attempt reconnection if it's disconnected
    /// </para>
    /// <para>
    /// <b>Default value is 5 seconds</b>
    /// </para>
    /// </summary>
    public int ReconnectInterval { get; init; } = 5;
}
