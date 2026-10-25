namespace app.Events;

/// <summary>
/// Defines an event key used for mapping
/// triggered automations in the inverted index.
/// </summary>
/// <remarks>
/// Implementations must provide robust equality
/// and hash-code semantics to function correctly as dictionary keys.
/// Value types, particularly readonly record struct types
/// containing value fields, are strongly recommended.
/// </remarks>
public interface IEvent { }
