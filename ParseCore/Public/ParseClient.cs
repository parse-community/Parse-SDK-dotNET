// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Common.Internal;
using Parse.Core.Internal;
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
      /// In the event that you would like to use the Parse SDK
      /// from a completely portable project, with no platform-specific library required,
      /// to get full access to all of our features available on Parse.com
      /// (A/B testing, slow queries, etc.), you must set the values of this struct
      /// to be appropriate for your platform.
      ///
      /// Any values set here will overwrite those that are automatically configured by
      /// any platform-specific migration library your app includes.
      /// </summary>
      public struct VersionInformation {
        /// <summary>
        /// The build number of your app.
        /// </summary>
        public String BuildVersion { get; set; }

        /// <summary>
        /// The human friendly version number of your happ.
        /// </summary>
        public String DisplayVersion { get; set; }

        /// <summary>
        /// The operating system version of the platform the SDK is operating in..
        /// </summary>
        public String OSVersion { get; set; }
      }

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

      /// <summary>
      /// Gets or sets additional HTTP headers to be sent with network requests from the SDK.
      /// </summary>
      public IDictionary<string, string> AdditionalHTTPHeaders { get; set; }

      /// <summary>
      /// The version information of your application environment.
      /// </summary>
      public VersionInformation VersionInfo { get; set; }
    }

    private static readonly object mutex = new object();

    static ParseClient() {
      versionString = "net-portable-" + Version;

      ParseModuleController.Instance.ScanForModules();
    }

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
        ParseObject.RegisterSubclass<ParseRole>();
        ParseObject.RegisterSubclass<ParseSession>();

        ParseModuleController.Instance.ParseDidInitialize();
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
  }
}
