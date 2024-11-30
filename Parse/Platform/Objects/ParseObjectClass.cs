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
        Constructor = Constructor = constructor;
        PropertyMappings = type.GetProperties().Select(property => (Property: property, FieldNameAttribute: property.GetCustomAttribute<ParseFieldNameAttribute>(true))).Where(set => set.FieldNameAttribute is { }).ToDictionary(set => set.Property.Name, set => set.FieldNameAttribute.FieldName);
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
            return Constructor.Invoke(new object[0]) as ParseObject;
        }
        else if (parameters.Length == 2 &&
                 parameters[0].ParameterType == typeof(string) &&
                 parameters[1].ParameterType == typeof(Parse.Abstractions.Infrastructure.IServiceHub))
        {
            // Two-parameter constructor
            string className = "_User"; // Replace with your desired class name
                                               // Validate className for the given type
                                               // Ensure ParseClient.Instance.Services is initialized
            var serviceHub = Parse.ParseClient.Instance.Services
                ?? throw new InvalidOperationException("ParseClient is not fully initialized.");


            if (!Parse.ParseClient.Instance.Services.ClassController.GetClassMatch(className, Constructor.DeclaringType))
            {
                throw new InvalidOperationException($"The className '{className}' is not valid for the type '{Constructor.DeclaringType}'.");
            }
            return Constructor.Invoke(new object[] { className, Parse.ParseClient.Instance.Services }) as ParseObject;
        }
        else
        {
            throw new InvalidOperationException("Unsupported constructor signature.");
        }
    }


    ConstructorInfo Constructor { get; }
}
