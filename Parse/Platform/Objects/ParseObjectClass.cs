using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parse.Abstractions.Internal;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Objects
{
    internal class ParseObjectClass
    {
        public ParseObjectClass(Type type, ConstructorInfo constructor)
        {
            TypeInfo = type.GetTypeInfo();
            DeclaredName = TypeInfo.GetParseClassName();
            Constructor = Constructor = constructor;
            PropertyMappings = type.GetProperties().Select(property => (Property: property, FieldNameAttribute: property.GetCustomAttribute<ParseFieldNameAttribute>(true))).Where(set => set.FieldNameAttribute is { }).ToDictionary(set => set.Property.Name, set => set.FieldNameAttribute.FieldName);
        }

        public TypeInfo TypeInfo { get; }

        public string DeclaredName { get; }

        public IDictionary<string, string> PropertyMappings { get; }

        public ParseObject Instantiate() => Constructor.Invoke(default) as ParseObject;

        ConstructorInfo Constructor { get; }
    }
}
