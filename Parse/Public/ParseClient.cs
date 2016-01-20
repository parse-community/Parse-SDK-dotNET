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
    internal static readonly string[] DateFormatStrings = {
      // Official ISO format
      "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'",

      // It's possible that the string converter server-side may trim trailing zeroes,
      // so these two formats cover ourselves from that.
      "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ff'Z'",
      "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'f'Z'",
    };

    /// <summary>
    /// Represents the configuration of the Parse SDK.
    /// </summary>
    public struct Configuration {
      /// <summary>
      /// The Parse.com application ID of your app.
      /// </summary>
      public String ApplicationId { get; set; }

      /// <summary>
      /// The Parse.com API server to connect to.
      /// 
      /// Only needs to be set if you're using another server than https://api.parse.com/1.
      /// </summary>
      public String Server { get; set; }

      /// <summary>
      /// The Parse.com .NET key for your app.
      /// </summary>
      public String WindowsKey { get; set; }
    }

    private static readonly object mutex = new object();
    private static readonly string[] assemblyNames = {
      "Parse.Phone", "Parse.WinRT", "Parse.UWP", "Parse.NetFx45", "Parse.iOS", "Parse.Android", "Parse.Unity"
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

    /// <summary>
    /// The current configuration that parse has been initialized with.
    /// </summary>
    public static Configuration CurrentConfiguration { get; internal set; }
    internal static string MasterKey { get; set; }

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
      Initialize(new Configuration {
        ApplicationId = applicationId,
        WindowsKey = dotnetKey
      });
    }

    /// <summary>
    /// Authenticates this client as belonging to your application. This must be
    /// called before your application can use the Parse library. The recommended
    /// way is to put a call to <c>ParseFramework.Initialize</c> in your
    /// Application startup.
    /// </summary>
    /// <param name="configuration">The configuration to initialize Parse with.
    /// </param>
    public static void Initialize(Configuration configuration) {
      lock (mutex) {
        configuration.Server = configuration.Server ?? "https://api.parse.com/1/";
        CurrentConfiguration = configuration;

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
