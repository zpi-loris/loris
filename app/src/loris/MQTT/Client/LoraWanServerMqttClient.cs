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

public class LoraWanServerMqttClient : BackgroundService
{
    private readonly ILogger<LoraWanServerMqttClient> _logger;

    private readonly ChannelWriter<IDeviceMessage> _deviceChannelWriter;

    // TODO: use environmental variables
    private readonly string _host = "192.168.10.111";
    private readonly int _port = 1883;
    private readonly int _reconectEverySecond = 5;
    private readonly string _topic = "application/+/device/+/event/up";

    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly MqttClientSubscribeOptions _mqttClientSubscribeOptions;

    public LoraWanServerMqttClient(
        ClientSettings settings,
        ILogger<LoraWanServerMqttClient> logger,
        ChannelWriter<IDeviceMessage> deviceChannelWriter
    )
    {
        _logger = logger;
        _deviceChannelWriter = deviceChannelWriter;

        var mqttFactory = new MqttClientFactory();

        _mqttClient = mqttFactory.CreateMqttClient();

        _mqttClientOptions = mqttFactory
            .CreateClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .Build();

        _mqttClientSubscribeOptions = mqttFactory
            .CreateSubscribeOptionsBuilder()
            .WithTopicFilter(t => t.WithTopic(_topic))
            .Build();
    }

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
    /// Responsible for stopping subscribe and message handlers<br/>
    /// after the client is disconnected<br/>
    /// <remarks>
    /// <b>Note that this not stop the background service</b>
    /// </remarks>
    /// </summary>
    private CancellationTokenSource? _disconnectedCts;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient.DisconnectedAsync += HandleDisconnected;
        _mqttClient.ApplicationMessageReceivedAsync += message =>
            HandleMessage(message, cancellationToken);

        return base.StartAsync(cancellationToken);
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
                if (_disconnectedCts.IsCancellationRequested == true)
                {
                    _logger.IntegrationDisconnected(_host, _port);
                    continue;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                _disconnectedCts.Dispose();
            }
        }
    }

    private async Task ConnectAndSubscribe(CancellationToken stoppingToken)
    {
        _logger.ConnectingToBroker(_host, _port);
        try
        {
            if (!await _mqttClient.TryPingAsync(stoppingToken))
            {
                await _mqttClient.ConnectAsync(_mqttClientOptions, stoppingToken);

                _logger.LogInformation("Client is connected");

                await _mqttClient.SubscribeAsync(_mqttClientSubscribeOptions, stoppingToken);

                _threadInControl.WaitOne();
            }
        }
        catch (MQTTnet.Exceptions.MqttCommunicationException)
        {
            _logger.IntegrationUnreachable(_host, _port);
        }
        finally
        {
            await Task.Delay(_reconectEverySecond * 1000, stoppingToken);
        }
    }

    private async Task HandleDisconnected(MqttClientDisconnectedEventArgs details)
    {
        if (details.ClientWasConnected)
        {
            // drop active subscribtion
            _disconnectedCts?.Cancel();

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
