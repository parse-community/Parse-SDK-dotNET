using System;
using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Objects;

public interface IParseObjectClassController
{
    string GetClassName(Type type);

    Type GetType(string className);

    bool GetClassMatch(string className, Type type);

    void AddValid(Type type);

    void RemoveClass(Type type);

    void AddRegisterHook(Type type, Action action);

    ParseObject Instantiate(string className, IServiceHub serviceHub);

    IDictionary<string, string> GetPropertyMappings(string className);

    void AddIntrinsic();
}
