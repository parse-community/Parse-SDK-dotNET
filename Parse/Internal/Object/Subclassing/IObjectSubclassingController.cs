using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Internal {
  internal interface IObjectSubclassingController {
    String GetClassName(Type type);
    Type GetType(String className);

    bool IsTypeValid(String className, Type type);

    void RegisterSubclass(Type t);
    void UnregisterSubclass(Type t);

    ParseObject Instantiate(String className);
    IDictionary<String, String> GetPropertyMappings(String className);
  }
}
