namespace Loris.Messages.Database;

/// <summary>
/// Defines a database persistence contract for messages
/// routed through the database write channel.
/// </summary>
/// <remarks>
/// Inverts persistence responsibility by delegating
/// state mapping and entity persistence logic
/// directly to the implementing message type
/// rather than the channel consumer.
/// </remarks>
public interface IDbWriteMessage { }
