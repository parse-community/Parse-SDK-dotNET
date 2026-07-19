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

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseLiveQueryEventArgs"/> class with the specified Parse object.
    /// </summary>
    /// <param name="current">The current state of the Parse object associated with the live query event.</param>
    /// <exception cref="ArgumentNullException"></exception>
    internal ParseLiveQueryEventArgs(ParseObject current) => Object = current ?? throw new ArgumentNullException(nameof(current));
}
