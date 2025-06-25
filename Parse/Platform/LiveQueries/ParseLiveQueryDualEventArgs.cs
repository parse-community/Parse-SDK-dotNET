using System;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Provides event arguments for events triggered by Parse's Live Query service.
/// This class encapsulates details about a particular event, such as the operation type,
/// client ID, request ID, and the associated Parse object data.
/// </summary>
public class ParseLiveQueryDualEventArgs : ParseLiveQueryEventArgs
{
    /// <summary>
    /// Gets the state of the Parse object before the live query event was triggered.
    /// This property represents the original data of the Parse object prior to any updates,
    /// providing a snapshot of its previous state for comparison purposes during events
    /// such as updates or deletes.
    /// </summary>
    public ParseObject Original { get; private set; }

    internal ParseLiveQueryDualEventArgs(ParseObject current, ParseObject original) : base(current) =>
        Original = original ?? throw new ArgumentNullException(nameof(original));
}
