using Microsoft.EntityFrameworkCore;

namespace app.Messages;

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
public interface IDbWriteMessage
{
    /// <summary>
    /// Persists the encapsulated message data to the database
    /// using the provided context.
    /// </summary>
    /// <param name="context">
    /// The database context used to apply entity state changes
    /// and execute writes.
    /// </param>
    public virtual async void SaveToDb(DbContext context) { }
}
