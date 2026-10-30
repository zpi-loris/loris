using System.Buffers;
using System.Text;
using System.Threading.Channels;
using Google.Protobuf;
using Loris.Logging;
using Loris.Messages;
using Loris.Protocol;
using Loris.Types;
using MQTTnet;
//
// ========
// Aliasese
// ========
//
// It is by design to alias to not confuse our events
// with chirpstack integration events
using ChirpstackUplink = Chirpstack.Integration.UplinkEvent;

namespace Loris.MQTT.Client;

public class LoraWanServerMqttClient(
    ILogger<LoraWanServerMqttClient> logger,
    ChannelWriter<IDeviceMessage> channelWriter
) : BackgroundService
{
    private readonly ILogger<LoraWanServerMqttClient> _logger = logger;
    private readonly ChannelWriter<IDeviceMessage> _deviceChannelWriter = channelWriter;

    // TODO: use environmental variables
    private readonly string _host = "192.168.10.111";
    private readonly int _port = 1883;
    private readonly int _reconectEverySecond = 5;
    private readonly string _topic = "application/+/device/+/event/up";

    private readonly MqttClientFactory _mqttFactory = new();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    private IMqttClient _mqttClient;
    private MqttClientOptions _mqttClientOptions;
    private MqttClientSubscribeOptions _mqttClientSubscribeOptions;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    // TODO describe threading
    private readonly ManualResetEvent _threadInControl = new(false);
    private CancellationTokenSource? _serviceConnected;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient = _mqttFactory.CreateMqttClient();
        _mqttClientOptions = _mqttFactory
            .CreateClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .Build();
        _mqttClientSubscribeOptions = _mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(t => t.WithTopic(_topic))
            .Build();

        _mqttClient.DisconnectedAsync += HandleDisconnected;
        _mqttClient.ApplicationMessageReceivedAsync += message =>
            HandleMessage(message, cancellationToken);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _serviceConnected = new CancellationTokenSource();

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _serviceConnected.Token,
                stoppingToken
            );

            _threadInControl.Reset();

            await ConnectAndSubscribe(linkedCts.Token, stoppingToken);
        }
    }

    private async Task ConnectAndSubscribe(
        CancellationToken linkedToken,
        CancellationToken externalToken
    )
    {
        try
        {
            _logger.ConnectingToBroker(_host, _port);
            if (!await _mqttClient.TryPingAsync(externalToken))
            {
                // throws if not successful
                await _mqttClient.ConnectAsync(_mqttClientOptions, externalToken);
                _logger.LogInformation("Client is connected");

                await _mqttClient.SubscribeAsync(_mqttClientSubscribeOptions, linkedToken);
                // lock thread
                _mrevent.WaitOne();
            }
        }
        catch (MQTTnet.Exceptions.MqttCommunicationException)
        {
            _logger.IntegrationUnreachable(_host, _port);
        }
        catch (OperationCanceledException)
        {
            if (_serviceConnected?.IsCancellationRequested == true)
            {
                _logger.IntegrationDisconnected(_host, _port);
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

    private void SetupDisconnectedHandling()
    {
        _mqttClient.DisconnectedAsync += async e =>
        {
            // drop active subscribtion
            _serviceConnected?.Cancel();

            _threadInControl.Set();
        };
    }

    private async Task HandleMessage(
        MqttApplicationMessageReceivedEventArgs message,
        CancellationToken stoppingToken
    )
    {
        var topic = message.ApplicationMessage.Topic;
        var mqttPayload = message.ApplicationMessage.Payload;

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

        // TODO: do some sort of topic routing

        await HandleUplink(mqttJsonPayload, stoppingToken);

        // if (topic == _topic)
        // {
        //     await HandleUplink(mqttJsonPayload, stoppingToken);
        // }
        // else if (topic == "someOtherTopic")
        // {
        //     Console.WriteLine("handling other payload");
        // }
    }

    private async Task HandleUplink(string json, CancellationToken stoppingToken)
    {
        if (TryExtractUplinkMessage(json, out var message))
        {
            _logger.UplinkReceived(_topic, message.Payload);

            await _deviceChannelWriter.WriteAsync(message, stoppingToken);
        }
    }

    private bool TryExtractUplinkMessage(string json, out UplinkMessage message)
    {
        message = default;

        // Decode chirpstack uplink message from MQTT
        ChirpstackUplink uplinkEvent;
        try
        {
            uplinkEvent = ChirpstackUplink.Parser.ParseJson(json);
        }
        catch (InvalidJsonException ex)
        {
            _logger.PayloadJsonParsingFailed(ex, json.Length);
            return false;
        }
        catch (InvalidProtocolBufferException ex)
        {
            _logger.PayloadProtobufSchemaMismatch(ex);
            return false;
        }

        // Decode loris protocol uplink from MQTT message payload
        Uplink uplink;
        try
        {
            uplink = Uplink.Parser.ParseFrom(uplinkEvent.Data);
        }
        catch (InvalidProtocolBufferException ex)
        {
            _logger.UplinkProtobufSchemaMismatch(ex);
            return false;
        }

        // Create Device Channel Uplink Message
        message = new UplinkMessage
        {
            Payload = uplink,
            DevEUI = DevEui.Parse(uplinkEvent.DeviceInfo.DevEui),
            DateTime = uplinkEvent.Time.ToDateTime(),
        };
        return true;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // release thread lock
        _threadInControl.Set();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
        _threadInControl.Dispose();
        _mqttClient.Dispose();
    }
}
