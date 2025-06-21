using System;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Provides event arguments for events triggered by Parse's Live Query service.
/// This class encapsulates details about a particular event, such as the operation type,
/// client ID, request ID, and the associated Parse object data.
/// </summary>
public class ParseLiveQueryEventArgs : EventArgs
{
    public object Object { get; set; }
    public object Original { get; set; }
}
