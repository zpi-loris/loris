using System.Buffers;
using System.Collections.Frozen;
using System.Text;
using Loris.Logging;
using Loris.MQTT.Extensions;
using MQTTnet;
using MQTTnet.Extensions.TopicTemplate;
using static Loris.MQTT.Utils;

namespace Loris.MQTT.Client;

public class ChirpstackIntegration(
    ChirpstackIntegrationSettings settings,
    IChirpstackIntegrationHandler handler,
    ILogger<ChirpstackIntegration> logger
) : BackgroundService
{
    private readonly ILogger<ChirpstackIntegration> _logger = logger;

    private readonly ChirpstackIntegrationSettings _settings = settings;

    private readonly FrozenDictionary<string, Func<string, CancellationToken, Task>> _routingTable =
        handler.CreateRoutingTable();

    private IMqttClient? _mqttClient;
    private MqttClientOptions? _mqttClientOptions;
    private MqttClientSubscribeOptions? _mqttClientSubscribeOptions;

    /// <summary>
    /// Allows to filter only one application from chirpstack.
    /// </summary>
    private static readonly string APP_ID_PLACEHOLDER = "applicationId";

    /// <summary>
    /// Allows to filter message type on MQTT chirpstack integration
    /// </summary>
    private static readonly string EVENT_PLACEHOLDER = "event";

    /// <summary>
    /// Chirpstack MQTT message topic template
    /// </summary>
    private static readonly MqttTopicTemplate _topicTemplate = new(
        $"application/{{{APP_ID_PLACEHOLDER}}}/device/+/event/{{{EVENT_PLACEHOLDER}}}"
    );

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var mqttFactory = new MqttClientFactory();

        _mqttClient = mqttFactory.CreateMqttClient();

        _mqttClientOptions = mqttFactory
            .CreateClientOptionsBuilder()
            .WithTcpServer(_settings.Host, _settings.Port)
            .Build();

        _mqttClientSubscribeOptions = CreateSubscribeOptionsFromTemplate(mqttFactory);

        _mqttClient.ApplicationMessageReceivedAsync += message =>
            HandleMessage(message, cancellationToken);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(_mqttClient, "MqttClient");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (await TryConnectAndSubscribe(stoppingToken))
            {
                await _mqttClient.WaitForDisconnectedAsync(stoppingToken);

                _logger.IntegrationDisconnected(_settings.Host, _settings.Port);
            }

            await Task.Delay(_settings.ReconnectInterval * 1000, stoppingToken);
        }
    }

    private MqttClientSubscribeOptions CreateSubscribeOptionsFromTemplate(
        MqttClientFactory mqttFactory
    )
    {
        var subscribeOptionsBuilder = mqttFactory.CreateSubscribeOptionsBuilder();

        MqttTopicTemplate template = _topicTemplate;
        foreach (var (eventName, _) in _routingTable)
        {
            if (_settings.ApplicationId is Guid value)
            {
                template = template.TrySetParameter(APP_ID_PLACEHOLDER, value.ToString());
            }

            template = template.TrySetParameter(EVENT_PLACEHOLDER, eventName);

            var topicFilter = template.TopicFilter;

            _logger.TopicSubscriptionConfigured(topicFilter);

            subscribeOptionsBuilder = subscribeOptionsBuilder.WithTopicFilter(topicFilter);
        }
        return subscribeOptionsBuilder.Build();
    }

    private async ValueTask<bool> TryConnectAndSubscribe(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(_mqttClient, "MqttClient");
        ArgumentNullException.ThrowIfNull(_mqttClientOptions, "MqttClientOptions");
        ArgumentNullException.ThrowIfNull(
            _mqttClientSubscribeOptions,
            "MqttClientSubscribeOptions"
        );

        _logger.ConnectingToBroker(_settings.Host, _settings.Port);

        try
        {
            if (!await _mqttClient.TryPingAsync(stoppingToken))
            {
                await _mqttClient.ConnectAsync(_mqttClientOptions, stoppingToken);

                _logger.ClientConnected(_settings.Host, _settings.Port);

                await _mqttClient.SubscribeAsync(_mqttClientSubscribeOptions, stoppingToken);
            }
        }
        catch (MQTTnet.Exceptions.MqttCommunicationException)
        {
            _logger.IntegrationUnreachable(_settings.Host, _settings.Port);

            return false;
        }

        return true;
    }

    private async Task HandleMessage(
        MqttApplicationMessageReceivedEventArgs message,
        CancellationToken stoppingToken
    )
    {
        var topic = message.ApplicationMessage.Topic;
        var mqttPayload = message.ApplicationMessage.Payload;

        _logger.MessageReceived(topic, mqttPayload.Length);

        // should always succeed
        string mqttJsonPayload;
        try
        {
            mqttJsonPayload = mqttPayload.IsSingleSegment
                ? Encoding.UTF8.GetString(mqttPayload.FirstSpan)
                : Encoding.UTF8.GetString(mqttPayload.ToArray());
        }
        catch (DecoderFallbackException ex)
        {
            _logger.PayloadUtf8DecodingFailed(ex, mqttPayload.Length);
            return;
        }

        var eventSpan = GetLastTopicSegmentSpan(topic);
        var lookup = _routingTable.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(eventSpan, out var handlerMethod))
        {
            await handlerMethod(mqttJsonPayload, stoppingToken);
        }
        else
        {
            _logger.NoHandlerRegisteredForTopic(eventSpan.ToString(), topic);
        }
    }
}
