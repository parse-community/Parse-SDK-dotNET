using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Core.Internal
{
    public interface IObjectSubclassingController
    {
        string GetClassName(Type type);
        Type GetType(string className);

        bool IsTypeValid(string className, Type type);

        void RegisterSubclass(Type t);
        void UnregisterSubclass(Type t);

        void AddRegisterHook(Type t, Action action);

        ParseObject Instantiate(string className);
        IDictionary<string, string> GetPropertyMappings(string className);
    }
}
