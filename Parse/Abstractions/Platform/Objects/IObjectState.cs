using System;
using System.Collections.Generic;
using Parse.Platform.Objects;

namespace Parse.Abstractions.Platform.Objects;

public interface IObjectState : IEnumerable<KeyValuePair<string, object>>
{
    bool IsNew { get; }
    string ClassName { get; set; }
    string ObjectId { get; }
    DateTime? UpdatedAt { get; }
    DateTime? CreatedAt { get; }
    object this[string key] { get; }

    bool ContainsKey(string key);

    IObjectState MutatedClone(Action<MutableObjectState> func);
}
