using Google.Protobuf;
using Loris.Protocol;

namespace Loris.Logging;

public static partial class IngestionLogging
{
    [LoggerMessage(
        EventId = 1201,
        EventName = nameof(ConnectingToBroker),
        Level = LogLevel.Information,
        Message = "Trying to connect to mqtt://{Host}:{Port}"
    )]
    public static partial void ConnectingToBroker(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1202,
        EventName = nameof(ClientConnected),
        Level = LogLevel.Information,
        Message = "Connected to MQTT broker at mqtt://{Host}:{Port}"
    )]
    public static partial void ClientConnected(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1301,
        EventName = nameof(UplinkReceived),
        Level = LogLevel.Debug,
        Message = "Uplink received on {Topic}: {Uplink}"
    )]
    public static partial void UplinkReceived(this ILogger logger, string topic, Uplink uplink);

    // Debug because this is dropped by design
    [LoggerMessage(
        EventId = 1302,
        EventName = nameof(UplinkProtobufSchemaMismatch),
        Level = LogLevel.Debug,
        Message = "Uplink bytes do not match Loris Uplink protobuf schema"
    )]
    public static partial void UplinkProtobufSchemaMismatch(
        this ILogger logger,
        InvalidProtocolBufferException ex
    );

    [LoggerMessage(
        EventId = 1303,
        EventName = nameof(TopicSubscriptionConfigured),
        Level = LogLevel.Debug,
        Message = "Configured MQTT topic filter: {TopicFilter}"
    )]
    public static partial void TopicSubscriptionConfigured(this ILogger logger, string topicFilter);

    [LoggerMessage(
        EventId = 1304,
        EventName = nameof(MessageReceived),
        Level = LogLevel.Debug,
        Message = "Received message on {Topic} ({PayloadLength} bytes)"
    )]
    public static partial void MessageReceived(
        this ILogger logger,
        string topic,
        long payloadLength
    );

    [LoggerMessage(
        EventId = 1305,
        EventName = nameof(StoppingIntegrationService),
        Level = LogLevel.Debug,
        Message = "Stopping Chirpstack MQTT integration background service"
    )]
    public static partial void StoppingIntegrationService(this ILogger logger);

    [LoggerMessage(
        EventId = 1401,
        EventName = nameof(IntegrationUnreachable),
        Level = LogLevel.Warning,
        Message = "mqtt//{Host}:{Port} is unreachable"
    )]
    public static partial void IntegrationUnreachable(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1402,
        EventName = nameof(IntegrationDisconnected),
        Level = LogLevel.Warning,
        Message = "mqtt://{Host}:{Port} disconnected"
    )]
    public static partial void IntegrationDisconnected(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1403,
        EventName = nameof(PayloadUtf8DecodingFailed),
        Level = LogLevel.Warning,
        Message = "Failed to decode integration payload: Invalid UTF-8 sequence (Length: {PayloadLength} bytes)"
    )]
    public static partial void PayloadUtf8DecodingFailed(
        this ILogger logger,
        Exception ex,
        long payloadLength
    );

    [LoggerMessage(
        EventId = 1404,
        EventName = nameof(PayloadJsonParsingFailed),
        Level = LogLevel.Warning,
        Message = "Failed to parse integration payload: Content is not valid JSON (Length: {PayloadLength} bytes)"
    )]
    public static partial void PayloadJsonParsingFailed(
        this ILogger logger,
        InvalidJsonException ex,
        long payloadLength
    );

    [LoggerMessage(
        EventId = 1405,
        EventName = nameof(PayloadProtobufSchemaMismatch),
        Level = LogLevel.Warning,
        Message = "JSON integration payload does not match UplinkEvent protobuf schema"
    )]
    public static partial void PayloadProtobufSchemaMismatch(
        this ILogger logger,
        InvalidProtocolBufferException ex
    );

    [LoggerMessage(
        EventId = 1406,
        EventName = nameof(NoHandlerRegisteredForTopic),
        Level = LogLevel.Warning,
        Message = "No matching route registered for event '{EventName}' on topic {Topic}"
    )]
    public static partial void NoHandlerRegisteredForTopic(
        this ILogger logger,
        string eventName,
        string topic
    );
}
