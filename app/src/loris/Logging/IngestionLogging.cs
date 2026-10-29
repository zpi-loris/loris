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
        EventName = nameof(UplinkEventReceived),
        Level = LogLevel.Debug,
        Message = "Uplink received on {Topic}: {Uplink}"
    )]
    public static partial void UplinkEventReceived(
        this ILogger logger,
        string topic,
        Uplink uplink
    );

    [LoggerMessage(
        EventId = 1004,
        EventName = nameof(ConnectingToBroker),
        Level = LogLevel.Information,
        Message = "Trying to connect to mqtt://{Host}:{Port}"
    )]
    public static partial void ConnectingToBroker(this ILogger logger, string host, int port);
}
