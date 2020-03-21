using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parse.Common.Internal;

namespace Parse.Core.Internal
{
    internal class ObjectSubclassInfo
    {
        public ObjectSubclassInfo(Type type, ConstructorInfo constructor)
        {
            TypeInfo = type.GetTypeInfo();
            ClassName = GetClassName(TypeInfo);
            Constructor = constructor;
            PropertyMappings = ReflectionHelpers.GetProperties(type).Select(prop => Tuple.Create(prop, prop.GetCustomAttribute<ParseFieldNameAttribute>(true))).Where(t => t.Item2 != null).Select(t => Tuple.Create(t.Item1, t.Item2.FieldName)).ToDictionary(t => t.Item1.Name, t => t.Item2);
        }

        public TypeInfo TypeInfo { get; private set; }
        public string ClassName { get; private set; }
        public IDictionary<string, string> PropertyMappings { get; private set; }
        private ConstructorInfo Constructor { get; set; }

        public ParseObject Instantiate() => (ParseObject) Constructor.Invoke(null);

        internal static string GetClassName(TypeInfo type) => type.GetCustomAttribute<ParseClassNameAttribute>()?.ClassName;
    }
}
