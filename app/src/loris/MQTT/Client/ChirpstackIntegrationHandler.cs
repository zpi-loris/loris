using System.Collections.Frozen;
using System.Threading.Channels;
using Google.Protobuf;
using Loris.Logging;
using Loris.Messages;
using Loris.Protocol;
using Loris.Types;
//
// It is by design to alias to not confuse our events
// with chirpstack integration events
using ChirpstackUplink = Chirpstack.Integration.UplinkEvent;

namespace Loris.MQTT.Client;

public class ChirpstackIntegrationHandler(
    ILogger<ChirpstackIntegrationHandler> logger,
    ChannelWriter<IDeviceMessage> channelWriter
) : IChirpstackIntegrationHandler
{
    private readonly ILogger<ChirpstackIntegrationHandler> _logger = logger;

    private readonly ChannelWriter<IDeviceMessage> _deviceChannelWriter = channelWriter;

    public IReadOnlyList<Func<string, CancellationToken, Task>> Subscriptions => [Up];

    public async Task Up(string jsonPayload, CancellationToken stoppingToken)
    {
        if (TryExtractUplinkMessage(jsonPayload, out var message))
        {
            // TODO: fix logger
            _logger.UplinkReceived("Debugging topic", message.Payload);

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
}
