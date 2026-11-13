namespace Loris.Messages;

/// <summary>
/// Marker interface for messages received
/// from the ChirpStack LoRaWAN network server.
/// </summary>
/// <remarks>
/// Implementations are passed through channels and are recommended
/// to be value types (such as readonly record structs)
/// to reduce allocation overhead.
/// </remarks>
public interface IDeviceMessage { }
