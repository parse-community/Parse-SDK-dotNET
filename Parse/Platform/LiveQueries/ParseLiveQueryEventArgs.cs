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
    /// Represents the event arguments provided to Live Query event handlers in the Parse platform.
    /// This class provides information about the current and original state of the Parse object
    /// involved in the Live Query operation.
    /// </summary>
    internal ParseLiveQueryEventArgs(ParseObject current, ParseObject original = null)
    {
        Object = current;
        Original = original;
    }

    /// <summary>
    /// Gets the current state of the Parse object associated with the live query event.
    /// This property provides the details of the Parse object as it existed at the time
    /// the event was triggered, reflecting any changes made during operations such as
    /// an update or creation.
    /// </summary>
    public ParseObject Object { get; private set; }

    /// <summary>
    /// Gets the state of the Parse object before the live query event was triggered.
    /// This property represents the original data of the Parse object prior to any updates,
    /// providing a snapshot of its previous state for comparison purposes during events
    /// such as updates or deletes.
    /// </summary>
    public ParseObject Original { get; private set; }
}
