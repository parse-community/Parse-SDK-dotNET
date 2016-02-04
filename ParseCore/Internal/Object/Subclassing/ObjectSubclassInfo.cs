using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  internal class ObjectSubclassInfo {
    public ObjectSubclassInfo(Type type, ConstructorInfo constructor) {
      TypeInfo = type.GetTypeInfo();
      ClassName = GetClassName(TypeInfo);
      Constructor = constructor;
      PropertyMappings = ReflectionHelpers.GetProperties(type)
        .Select(prop => Tuple.Create(prop, prop.GetCustomAttribute<ParseFieldNameAttribute>(true)))
        .Where(t => t.Item2 != null)
        .Select(t => Tuple.Create(t.Item1, t.Item2.FieldName))
        .ToDictionary(t => t.Item1.Name, t => t.Item2);
    }

    public TypeInfo TypeInfo { get; private set; }
    public String ClassName { get; private set; }
    public IDictionary<String, String> PropertyMappings { get; private set; }
    private ConstructorInfo Constructor { get; set; }

    public ParseObject Instantiate() {
      return (ParseObject)Constructor.Invoke(null);
    }

    internal static String GetClassName(TypeInfo type) {
      var attribute = type.GetCustomAttribute<ParseClassNameAttribute>();
      return attribute != null ? attribute.ClassName : null;
    }
  }
}
