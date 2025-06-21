using System;

namespace Parse.Platform.LiveQueries;

public class ParseLiveQueryErrorEventArgs : EventArgs
{
    public string Error { get; set; }
    public int Code { get; set; }
    public bool Reconnected { get; set; }
}