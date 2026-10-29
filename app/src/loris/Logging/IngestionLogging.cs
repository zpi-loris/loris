using Google.Protobuf;
using Loris.Protocol;

namespace Loris.Logging;

public static partial class IngestionLogging
{
    [LoggerMessage(
        EventId = 1001,
        EventName = nameof(IntegrationUnreachable),
        Level = LogLevel.Warning,
        Message = "Trying to connect to mqtt//{Host}:{Port}"
    )]
    public static partial void IntegrationUnreachable(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1002,
        EventName = nameof(IntegrationDisconnected),
        Level = LogLevel.Warning,
        Message = "mqtt://{Host}:{Port} disconnected"
    )]
    public static partial void IntegrationDisconnected(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1003,
        EventName = nameof(UplinkReceived),
        Level = LogLevel.Debug,
        Message = "Uplink received on {Topic}: {Uplink}"
    )]
    public static partial void UplinkReceived(this ILogger logger, string topic, Uplink uplink);

    [LoggerMessage(
        EventId = 1004,
        EventName = nameof(ConnectingToBroker),
        Level = LogLevel.Information,
        Message = "Trying to connect to mqtt://{Host}:{Port}"
    )]
    public static partial void ConnectingToBroker(this ILogger logger, string host, int port);

    [LoggerMessage(
        EventId = 1010,
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
        EventId = 1011,
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
        EventId = 1012,
        EventName = nameof(PayloadProtobufSchemaMismatch),
        Level = LogLevel.Warning,
        Message = "JSON integration payload does not match UplinkEvent protobuf schema"
    )]
    public static partial void PayloadProtobufSchemaMismatch(
        this ILogger logger,
        InvalidProtocolBufferException ex
    );

    // Debug because this is dropped by design
    [LoggerMessage(
        EventId = 1014,
        EventName = nameof(UplinkProtobufSchemaMismatch),
        Level = LogLevel.Debug,
        Message = "Uplink bytes do not match Loris Uplink protobuf schema"
    )]
    public static partial void UplinkProtobufSchemaMismatch(
        this ILogger logger,
        InvalidProtocolBufferException ex
    );
}
