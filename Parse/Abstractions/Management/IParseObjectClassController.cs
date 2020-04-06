using System;
using System.Collections.Generic;

namespace Parse.Core.Internal
{
    public interface IParseObjectClassController
    {
        string GetClassName(Type type);

        Type GetType(string className);

        bool GetClassMatch(string className, Type type);

        void AddValid(Type t);

        void RemoveClass(Type t);

        void AddRegisterHook(Type t, Action action);

        ParseObject Instantiate(string className);

        IDictionary<string, string> GetPropertyMappings(string className);

        void AddIntrinsic();
    }
}
