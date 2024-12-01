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
            string className = Constructor.DeclaringType?.Name ?? "_User";
            var serviceHub = Parse.ParseClient.Instance.Services;
            return Constructor.Invoke(new object[] { className, serviceHub }) as ParseObject;
        }

        throw new InvalidOperationException("Unsupported constructor signature.");
    }


    //public ParseObject Instantiate()
    //{
    //    var parameters = Constructor.GetParameters();

    //    //if (parameters.Length == 0)
    //    //{
    //    //    var plessCtor = Constructor.Invoke(new object[0]) as ParseObject;
    //    //    // Parameterless constructor
    //    //    return plessCtor;
    //    //}
    //    //else
    //    if (parameters.Length == 2 &&
    //             parameters[0].ParameterType == typeof(string) &&
    //             parameters[1].ParameterType == typeof(Parse.Abstractions.Infrastructure.IServiceHub))
    //    {
    //        // Two-parameter constructor
    //        string className; // Default to "_User" for ParseUser
    //        if (Constructor.DeclaringType == typeof(ParseUser))
    //            className =  "_User";
    //        else
    //            className =  "_User";

    //        // Validate ParseClient.Instance.Services is initialized
    //        var serviceHub = Parse.ParseClient.Instance.Services
    //            ?? throw new InvalidOperationException("ParseClient is not fully initialized.");

    //        // Validate the className for the given type
    //        if (!serviceHub.ClassController.GetClassMatch(className, Constructor.DeclaringType))
    //        {
    //            throw new InvalidOperationException($"The className '{className}' is not valid for the type '{Constructor.DeclaringType}'.");
    //        }

    //        // Invoke the constructor with className and serviceHub
    //        return Constructor.Invoke(new object[] { className, serviceHub }) as ParseObject;
    //    }
    //    else
    //    {
    //        throw new InvalidOperationException("Unsupported constructor signature.");
    //    }
    //}



    ConstructorInfo Constructor { get; }
}
