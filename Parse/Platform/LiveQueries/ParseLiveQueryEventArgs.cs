using System;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Provides the data object for events triggered by Parse's Live Query service.
/// </summary>
public class ParseLiveQueryEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current state of the Parse object associated with the live query event.
    /// This property provides the details of the Parse object as it existed at the time
    /// the event was triggered, reflecting any changes made during operations such as
    /// an update or creation.
    /// </summary>
    public ParseObject Object { get; }

    internal ParseLiveQueryEventArgs(ParseObject current) => Object = current ?? throw new ArgumentNullException(nameof(current));
}
