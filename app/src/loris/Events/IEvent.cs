namespace Loris.Events;

/// <summary>
/// Defines an event key used for mapping
/// triggered automations in the inverted index.<br/>
/// </summary>
/// <remarks><b>It is not intended to be implemented !</b></remarks>
public interface IEvent { }

/// <summary>
/// This interface allow types to serve as automation inverted index keys.<br/>
/// Contradictory to <b>IEvent</b> - this interface is meant to be implemented
/// </summary>
/// <remarks>
/// Implementations must provide robust equality
/// and hash-code semantics to function correctly as dictionary keys.<br/>
/// <code>
/// record          // class - reference
/// record struct   // value type
/// </code>
/// Implements this interface out of the box.
/// </remarks>
public interface IEvent<T> : IEvent, IEquatable<T>
    where T : IEvent<T> { }
