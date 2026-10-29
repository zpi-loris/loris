using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
// using Chirpstack.Integration;
using Google.Protobuf;
using Loris.Protocol;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;

namespace Loris.MQTT.Client;

public class LoraWanServerMqttClient(ILogger<LoraWanServerMqttClient> logger) : BackgroundService
{
    private readonly ILogger<LoraWanServerMqttClient> _logger = logger;

    // TODO: use environmental variables
    private readonly string _host = "192.168.10.111";
    private readonly int _port = 1883;
    private readonly int _reconectEverySecond = 5;
    private readonly string _topic = "application/+/device/+/event/up";

    private readonly ManualResetEvent _mrevent = new(false);
    private CancellationTokenSource? _serviceConnected;
    private readonly MqttClientFactory _mqttFactory = new();
    private readonly JsonParser _jsonParser = JsonParser.Default;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var mqttClient = _mqttFactory.CreateMqttClient();

        var mqttClientOptions = _mqttFactory
            .CreateClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .Build();
        var topicFilter = _mqttFactory.CreateTopicFilterBuilder().WithTopic(_topic).Build();
        var mqttSubscribeOptions = _mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter)
            .Build();

        mqttClient.DisconnectedAsync += async e =>
        {
            // drop active subscribtion
            _serviceConnected?.Cancel();

            // resume connection loop by releasing thread lock
            _mrevent.Set();
        };

        SetupMessageHandling(mqttClient);

        while (!stoppingToken.IsCancellationRequested)
        {
            _serviceConnected = new CancellationTokenSource();

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _serviceConnected.Token,
                stoppingToken
            );

            _mrevent.Reset();

            await ConnectAndSubscribe(
                mqttClient,
                mqttClientOptions,
                mqttSubscribeOptions,
                linkedCts.Token,
                stoppingToken
            );
        }
    }

    private async Task ConnectAndSubscribe(
        IMqttClient mqttClient,
        MqttClientOptions mqttClientOptions,
        MqttClientSubscribeOptions mqttSubscribeOptions,
        CancellationToken linkedToken,
        CancellationToken externalToken
    )
    {
        try
        {
            _logger.LogInformation("Trying to connect to {host}:{port}", _host, _port);
            if (!await mqttClient.TryPingAsync(externalToken))
            {
                // throws if not successful
                await mqttClient.ConnectAsync(mqttClientOptions, externalToken);
                _logger.LogInformation("Client is connected");

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, linkedToken);
                // lock thread
                _mrevent.WaitOne();
            }
        }
        catch (MQTTnet.Exceptions.MqttCommunicationException)
        {
            _logger.LogError("Can't reach the broker {host}:{port}", _host, _port);
        }
        catch (OperationCanceledException)
        {
            if (_serviceConnected?.IsCancellationRequested == true)
            {
                _logger.LogInformation("broker {host}:{port} disconnected", _host, _port);
                _serviceConnected.Dispose();
            }
            else
            {
                // rethrow host cancellation
                throw;
            }
        }
        finally
        {
            await Task.Delay(_reconectEverySecond * 1000, externalToken);
        }
    }

    private void SetupMessageHandling(IMqttClient mqttClient)
    {
        mqttClient.ApplicationMessageReceivedAsync += message =>
        {
            try
            {
                var json = JsonDocument.Parse(message.ApplicationMessage.Payload);
                var uplinkEvent = JsonSerializer.Deserialize(
                    json,
                    SourceGenerationContext.Default.UplinkEventDto
                );
                // var UplinkEventBytes = message.ApplicationMessage.Payload;
                // var uplinkEventJson = UTF8Encoding.UTF8.GetString(UplinkEventBytes);
                // var uplinkEvent = _jsonParser.Parse<UplinkEvent>(uplinkEventJson);
                var uplinkBytes = uplinkEvent.Data;
                var uplinkJson = Encoding.UTF8.GetString(uplinkBytes);
                var uplink = _jsonParser.Parse<Uplink>(uplinkJson);

                _logger.LogInformation("Uplink received on {topic}: {uplink}", _topic, uplink);

                // TODO: parse, and push to channel
                // _logger.LogInformation(
                //     "Uplink received on {topic}: {uplink}",
                //     message.ApplicationMessage.Topic,
                //     uplink
                // );
            }
            // TODO: better error handling
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return Task.CompletedTask;
        };
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // release thread lock
        _mrevent.Set();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
        _serviceConnected?.Dispose();
        _mrevent.Dispose();
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UplinkEventDto))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

public record UplinkEventDto
{
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; init; }

    [JsonPropertyName("deviceInfo")]
    public DeviceInfoDto DeviceInfo { get; init; } = default!;

    [JsonPropertyName("devAddr")]
    public string DevAddr { get; init; } = string.Empty;

    /// <summary>
    /// Base64 encoded payload
    /// </summary>
    [JsonPropertyName("data")]
    public byte[] Data { get; init; } = [];
}

public record DeviceInfoDto
{
    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; init; }

    [JsonPropertyName("tenantName")]
    public string TenantName { get; init; } = string.Empty;

    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; init; }

    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; init; } = string.Empty;

    [JsonPropertyName("deviceProfileId")]
    public Guid DeviceProfileId { get; init; }

    [JsonPropertyName("deviceProfileName")]
    public string DeviceProfileName { get; init; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; init; } = string.Empty;

    [JsonPropertyName("devEui")]
    public string DevEui { get; init; } = string.Empty;
}

// sealed class MyLogger(ILogger<LoraWanServerMqttClient> logger) : IMqttNetLogger
// {
//     public bool IsEnabled { get; set; } = true;
//
//     private readonly ILogger<LoraWanServerMqttClient> _logger = logger;
//
//     public void Publish(
//         MqttNetLogLevel level,
//         string source,
//         string message,
//         object[] parameters,
//         Exception exception
//     )
//     {
//         Action log = level switch
//         {
//             MqttNetLogLevel.Info => () =>
//                 _logger.LogInformation(
//                     exception: exception,
//                     message: message,
//                     args: parameters
//                 ),
//             MqttNetLogLevel.Warning => () =>
//                 _logger.LogWarning(exception: exception, message: message, args: parameters),
//             MqttNetLogLevel.Error => () =>
//                 _logger.LogError(exception: exception, message: message, args: parameters),
//             MqttNetLogLevel.Verbose => () =>
//                 _logger.LogDebug(exception: exception, message: message, args: parameters),
//             _ => () => { },
//         };
//
//         log();
//     }
// }
