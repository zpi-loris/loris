using System.Buffers;
using System.Collections.Frozen;
using System.Text;
using Loris.Logging;
using MQTTnet;
using MQTTnet.Extensions.TopicTemplate;

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
    /// This field is responsible for thread synchronization signalling.<br/>
    /// The background service is calling the .WaitOne() method and one of two<br/>
    /// behaviours occur, based on the state of the event:<br/><br/>
    ///
    /// - signalled (.Set() or true) - thread is blocked<br/>
    /// - unsignalled (.Reset() or false) - thread is free<br/>
    /// </summary>
    private readonly ManualResetEvent _threadInControl = new(false);

    /// <summary>
    /// Responsible for stopping active <b>subscribe</b><br/>
    /// after the client is disconnected<br/>
    /// <para>
    /// <b>Note that this not stop the background service</b>
    /// </para>
    /// </summary>
    private CancellationTokenSource? _disconnectedCts;

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

        _mqttClient.DisconnectedAsync += HandleDisconnected;
        _mqttClient.ApplicationMessageReceivedAsync += message =>
            HandleMessage(message, cancellationToken);

        return base.StartAsync(cancellationToken);
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

            var topicFilter = template.BuildFilter();

            // TODO: DEBUG created topic subscription or sth like that

            subscribeOptionsBuilder = subscribeOptionsBuilder.WithTopicFilter(
                template.BuildFilter()
            );
        }
        return subscribeOptionsBuilder.Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _threadInControl.Reset();

            _disconnectedCts = new CancellationTokenSource();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _disconnectedCts.Token,
                stoppingToken
            );

            try
            {
                await ConnectAndSubscribe(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (_disconnectedCts.IsCancellationRequested)
                {
                    // TODO: INFO client is disconnected
                }
            }
            finally
            {
                // each iteration there is new token source created
                _disconnectedCts.Dispose();
            }
        }
    }

    private async Task ConnectAndSubscribe(CancellationToken stoppingToken)
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

                // TODO: INFO client is connected
                _logger.LogInformation("Client is connected");

                await _mqttClient.SubscribeAsync(_mqttClientSubscribeOptions, stoppingToken);

                _threadInControl.WaitOne();
            }
        }
        catch (MQTTnet.Exceptions.MqttCommunicationException)
        {
            _logger.IntegrationUnreachable(_settings.Host, _settings.Port);
        }
        finally
        {
            await Task.Delay(_settings.ReconnectInterval * 1000, stoppingToken);
        }
    }

    private async Task HandleDisconnected(MqttClientDisconnectedEventArgs details)
    {
        if (details.ClientWasConnected)
        {
            // drop active subscribtion
            _disconnectedCts?.Cancel();

            // resume thread
            _threadInControl.Set();
        }
    }

    private async Task HandleMessage(
        MqttApplicationMessageReceivedEventArgs message,
        CancellationToken stoppingToken
    )
    {
        var topic = message.ApplicationMessage.Topic;
        var mqttPayload = message.ApplicationMessage.Payload;

        // TODO: DEBUG get message on topic with n bytes

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
            // TODO: WARNING received message on topic with no registered handler
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // TODO: DEBUG stop logging

        // release thread lock
        _threadInControl.Set();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
        _threadInControl.Dispose();
        _mqttClient?.Dispose();
    }

    private static ReadOnlySpan<char> GetLastTopicSegmentSpan(ReadOnlySpan<char> topic)
    {
        int lastSlashIndex = topic.LastIndexOf('/');
        return lastSlashIndex >= 0 ? topic[(lastSlashIndex + 1)..] : topic;
    }
}
