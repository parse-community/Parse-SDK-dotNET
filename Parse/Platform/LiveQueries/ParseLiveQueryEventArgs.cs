using System;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Provides event arguments for events triggered by Parse's Live Query service.
/// This class encapsulates details about a particular event, such as the operation type,
/// client ID, request ID, and the associated Parse object data.
/// </summary>
public class ParseLiveQueryEventArgs : EventArgs
{
    /// <summary>
    /// Event arguments for events triggered by Parse's Live Query service.
    /// </summary>
    /// <remarks>
    /// This class handles the encapsulation of event-related details for Live Query operations,
    /// such as the current Parse object state and the original state before updates.
    /// </remarks>
    internal ParseLiveQueryEventArgs(object current, object original = null)
    {
        Object = current;
        Original = original;
    }

    /// <summary>
    /// Gets the associated object involved in the live query event.
    /// This property provides the current state of the Parse object
    /// that triggered the event. The object may vary depending on the live query event
    /// type, such as create, update, enter, or leave.
    /// </summary>
    public object Object { get; private set; }

    /// <summary>
    /// Gets the original state of the Parse object before the live query event occurred.
    /// This property holds the state of the object as it was prior to the event that
    /// triggered the live query event, such as an update or leave operation.
    /// </summary>
    public object Original { get; private set; }
}
