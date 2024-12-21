using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Parse.Abstractions.Internal;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Objects;

internal class ParseObjectClass
{
    public ParseObjectClass(Type type, ConstructorInfo constructor)
    {
        TypeInfo = type.GetTypeInfo();
        DeclaredName = TypeInfo.GetParseClassName();
        Constructor = constructor;
        PropertyMappings = type.GetProperties()
            .Select(property => (Property: property, FieldNameAttribute: property.GetCustomAttribute<ParseFieldNameAttribute>(true)))
            .Where(set => set.FieldNameAttribute is { })
            .ToDictionary(set => set.Property.Name, set => set.FieldNameAttribute.FieldName);
    }

    public TypeInfo TypeInfo { get; }

    public string DeclaredName { get; }

    public IDictionary<string, string> PropertyMappings { get; }

    public ParseObject Instantiate()
    {
        var parameters = Constructor.GetParameters();
        
        if (parameters.Length == 0)
        {            
            
            // Parameterless constructor
            return Constructor.Invoke(null) as ParseObject;
        }
        else if (parameters.Length == 2 &&
                 parameters[0].ParameterType == typeof(string) &&
                 parameters[1].ParameterType == typeof(Parse.Abstractions.Infrastructure.IServiceHub))
        {
         

            // Two-parameter constructor
            string className = Constructor.DeclaringType?.Name ?? "_User"; //Still Unsure about this default value, maybe User is not the best choice, but what else?
            var serviceHub = Parse.ParseClient.Instance.Services;
            return Constructor.Invoke(new object[] { className, serviceHub }) as ParseObject;
        }
        

        throw new InvalidOperationException("Unsupported constructor signature.");
    }

    ConstructorInfo Constructor { get; }
}
