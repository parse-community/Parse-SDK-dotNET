// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse {
  /// <summary>
  /// ParseClient contains static functions that handle global
  /// configuration for the Parse library.
  /// </summary>
  public static partial class ParseClient {
    internal const string DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

    private static readonly object mutex = new object();
    private static readonly string[] assemblyNames = {
      "Parse.Phone", "Parse.WinRT", "Parse.NetFx45", "Parse.iOS", "Parse.Android", "Parse.Unity"
    };

    static ParseClient() {
      Type platformHookType = GetParseType("PlatformHooks");
      if (platformHookType == null) {
        throw new InvalidOperationException("You must include a reference to a platform-specific Parse library.");
      }
      platformHooks = Activator.CreateInstance(platformHookType) as IPlatformHooks;
      commandRunner = new ParseCommandRunner(platformHooks.HttpClient);
      versionString = "net-" + platformHooks.SDKName + Version;
    }

    private static Type GetParseType(string name) {
      foreach (var assembly in assemblyNames) {
        Type type = Type.GetType(string.Format("Parse.{0}, {1}", name, assembly));
        if (type != null) {
          return type;
        }
      }
      return null;
    }

    private static readonly IPlatformHooks platformHooks;
    internal static IPlatformHooks PlatformHooks { get { return platformHooks; } }

    private static readonly IParseCommandRunner commandRunner;
    internal static IParseCommandRunner ParseCommandRunner { get { return commandRunner; } }

    internal static Uri HostName { get; set; }
    internal static string MasterKey { get; set; }
    internal static string ApplicationId { get; set; }
    internal static string WindowsKey { get; set; }

    internal static Version Version {
      get {
        var assemblyName = new AssemblyName(typeof(ParseClient).GetTypeInfo().Assembly.FullName);
        return assemblyName.Version;
      }
    }

    private static readonly string versionString;
    internal static string VersionString {
      get {
        return versionString;
      }
    }

    /// <summary>
    /// Authenticates this client as belonging to your application. This must be
    /// called before your application can use the Parse library. The recommended
    /// way is to put a call to <c>ParseFramework.Initialize</c> in your
    /// Application startup.
    /// </summary>
    /// <param name="applicationId">The Application ID provided in the Parse dashboard.
    /// </param>
    /// <param name="dotnetKey">The .NET API Key provided in the Parse dashboard.
    /// </param>
    public static void Initialize(string applicationId, string dotnetKey) {
      lock (mutex) {
        HostName = HostName ?? new Uri("https://api.parse.com/1/");
        ApplicationId = applicationId;
        WindowsKey = dotnetKey;

        ParseObject.RegisterSubclass<ParseUser>();
        ParseObject.RegisterSubclass<ParseInstallation>();
        ParseObject.RegisterSubclass<ParseRole>();
        ParseObject.RegisterSubclass<ParseSession>();

        // Give platform-specific libraries a chance to do additional initialization.
        PlatformHooks.Initialize();
      }
    }

    internal static Guid? InstallationId {
      get {
        return ParseCorePlugins.Instance.InstallationIdController.Get();
      }
      set {
        ParseCorePlugins.Instance.InstallationIdController.Set(value);
      }
    }

    /// <summary>
    /// Performs a ConvertTo, but returns null if the object can't be
    /// converted to that type.
    /// </summary>
    internal static T As<T>(object value) where T : class {
      return ConvertTo<T>(value) as T;
    }

    /// <summary>
    /// Converts a value to the requested type -- coercing primitives to
    /// the desired type, wrapping lists and dictionaries appropriately,
    /// or else passing the object along to the caller unchanged.
    /// 
    /// This should be used on any containers that might be coming from a
    /// user to normalize the collection types. Collection types coming from
    /// JSON deserialization can be safely assumed to be lists or dictionaries of
    /// objects.
    /// </summary>
    internal static object ConvertTo<T>(object value) {
      if (value is T || value == null) {
        return value;
      }

      if (typeof(T).IsPrimitive()) {
        return (T)Convert.ChangeType(value, typeof(T));
      }

      if (typeof(T).IsConstructedGenericType()) {
        // Add lifting for nullables. Only supports conversions between primitives.
        if (typeof(T).IsNullable()) {
          var innerType = typeof(T).GetGenericTypeArguments()[0];
          if (innerType.IsPrimitive()) {
            return (T)Convert.ChangeType(value, innerType);
          }
        }
        Type listType = GetInterfaceType(value.GetType(), typeof(IList<>));
        if (listType != null &&
            typeof(T).GetGenericTypeDefinition() == typeof(IList<>)) {
          var wrapperType = typeof(FlexibleListWrapper<,>).MakeGenericType(typeof(T).GetGenericTypeArguments()[0],
              listType.GetGenericTypeArguments()[0]);
          return Activator.CreateInstance(wrapperType, value);
        }
        Type dictType = GetInterfaceType(value.GetType(), typeof(IDictionary<,>));
        if (dictType != null &&
            typeof(T).GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
          var wrapperType = typeof(FlexibleDictionaryWrapper<,>).MakeGenericType(typeof(T).GetGenericTypeArguments()[1],
              dictType.GetGenericTypeArguments()[1]);
          return Activator.CreateInstance(wrapperType, value);
        }
      }

      return value;
    }

    /// <summary>
    /// Holds a dictionary that maps a cache of interface types for related concrete types.
    /// The lookup is slow the first time for each type because it has to enumerate all interface
    /// on the object type, but made fast by the cache.
    /// 
    /// The map is:
    ///    (object type, generic interface type) => constructed generic type
    /// </summary>
    private static readonly Dictionary<Tuple<Type, Type>, Type> interfaceLookupCache =
        new Dictionary<Tuple<Type, Type>, Type>();
    private static Type GetInterfaceType(Type objType, Type genericInterfaceType) {
      // Side note: It so sucks to have to do this. What a piece of crap bit of code
      // Unfortunately, .NET doesn't provide any of the right hooks to do this for you
      // *sigh*
      if (genericInterfaceType.IsConstructedGenericType()) {
        genericInterfaceType = genericInterfaceType.GetGenericTypeDefinition();
      }
      var cacheKey = new Tuple<Type, Type>(objType, genericInterfaceType);
      if (interfaceLookupCache.ContainsKey(cacheKey)) {
        return interfaceLookupCache[cacheKey];
      }
      foreach (var type in objType.GetInterfaces()) {
        if (type.IsConstructedGenericType() &&
            type.GetGenericTypeDefinition() == genericInterfaceType) {
          return interfaceLookupCache[cacheKey] = type;
        }
      }
      return null;
    }

    internal static string BuildQueryString(IDictionary<string, object> parameters) {
      return string.Join("&", (from pair in parameters
                               let valueString = pair.Value as string
                               select string.Format("{0}={1}",
                                 Uri.EscapeDataString(pair.Key),
                                 Uri.EscapeDataString(string.IsNullOrEmpty(valueString) ?
                                    Json.Encode(pair.Value) : valueString)))
                                 .ToArray());
    }

    internal static IDictionary<string, string> DecodeQueryString(string queryString) {
      var dict = new Dictionary<string, string>();
      foreach (var pair in queryString.Split('&')) {
        var parts = pair.Split(new char[] { '=' }, 2);
        dict[parts[0]] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : null;
      }
      return dict;
    }

    internal static IDictionary<string, object> DeserializeJsonString(string jsonData) {
      return Json.Parse(jsonData) as IDictionary<string, object>;
    }

    internal static string SerializeJsonString(IDictionary<string, object> jsonData) {
      return Json.Encode(jsonData);
    }

    internal static IDictionary<string, object> ApplicationSettings {
      get {
        return PlatformHooks.ApplicationSettings;
      }
    }
  }
}
