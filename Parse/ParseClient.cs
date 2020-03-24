// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parse.Abstractions.Library;
using Parse.Abstractions.Management;
using Parse.Abstractions.Storage;
using Parse.Common.Internal;
using Parse.Library;
using Parse.Management;

namespace Parse
{
    /// <summary>
    /// ParseClient contains static functions that handle global
    /// configuration for the Parse library.
    /// </summary>
    public static partial class ParseClient
    {
        /// <summary>
        /// Contains, in order, the official ISO date and time format strings, and two modified versions that account for the possibility that the server-side string processing mechanism removed trailing zeroes.
        /// </summary>
        internal static string[] DateFormatStrings { get; } =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ff'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'f'Z'",
        };

        static object Mutex { get; } = new object { };

        /// <summary>
        /// The current configuration that parse has been initialized with.
        /// </summary>
        public static Configuration Configuration { get; internal set; }

        internal static Version Version => new AssemblyName(typeof(ParseClient).GetTypeInfo().Assembly.FullName).Version;

        /// <summary>
        /// Authenticates this client as belonging to your application. This must be
        /// called before your application can use the Parse library. The recommended
        /// way is to put a call to <c>ParseClient.Initialize</c> in your
        /// Application startup.
        /// </summary>
        /// <param name="identifier">The Application ID provided in the Parse dashboard.
        /// </param>
        /// <param name="serverURI">The server URI provided in the Parse dashboard.
        /// </param>
        public static void Initialize(string identifier, string serverURI, ICacheLocationConfiguration storageConfiguration = default, IHostApplicationVersioningData hostVersioning = default, IParseCorePlugins plugins = default) => Initialize(new Configuration { ApplicationID = identifier, ServerURI = serverURI }, storageConfiguration, hostVersioning, plugins);

        /// <summary>
        /// Authenticates this client as belonging to your application. This must be
        /// called before your application can use the Parse library. The recommended
        /// way is to put a call to <c>ParseClient.Initialize</c> in your
        /// Application startup.
        /// </summary>
        /// <param name="configuration">The configuration to initialize Parse with.
        /// </param>
        public static void Initialize(Configuration configuration, ICacheLocationConfiguration cacheConfiguration = default, IHostApplicationVersioningData hostVersioning = default, IParseCorePlugins plugins = default)
        {
            lock (Mutex)
            {
                configuration.ServerURI ??= configuration.Test ? "https://api.parse.com/1/" : throw new ArgumentException("Since the official parse server has shut down, you must specify a URI that points to a hosted instance.");
                plugins ??= new ParseCorePlugins { };

                bool keepRelativeStoragePath = plugins is { StorageController: { } }, keepVersion = plugins is { MetadataController: { } };

                if (plugins is ParseCorePlugins)
                {
                    ParseCorePlugins.Instance = plugins;
                }

                if (hostVersioning is { } && !keepVersion && plugins.MetadataController is MetadataController { } metadataController)
                {
                    metadataController.HostVersioningData = hostVersioning switch
                    {
                        { CanBeUsedForInference: true } data => data,
                        { IsDefault: false } data => new HostApplicationVersioningData
                        {
                            BuildVersion = data.BuildVersion,
                            HostOSVersion = data.HostOSVersion,
                            DisplayVersion = HostApplicationVersioningData.Inferred.DisplayVersion
                        },
                        _ => HostApplicationVersioningData.Inferred
                    };
                }

                if (cacheConfiguration is { } && !keepRelativeStoragePath && plugins.StorageController is StorageController { } storageController)
                {
                    storageController.RelativeStorageFilePath = cacheConfiguration.GetRelativeStorageFilePath(plugins);
                }

                Configuration = configuration;

                ParseObject.RegisterDerivative<ParseUser>();
                ParseObject.RegisterDerivative<ParseRole>();
                ParseObject.RegisterDerivative<ParseSession>();
                ParseObject.RegisterDerivative<ParseInstallation>();

                AppDomain.CurrentDomain.ProcessExit += (_, __) => plugins.StorageController.Clean();
            }
        }

        internal static string BuildQueryString(IDictionary<string, object> parameters) => String.Join("&", (from pair in parameters let valueString = pair.Value as string select $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(String.IsNullOrEmpty(valueString) ? Json.Encode(pair.Value) : valueString)}").ToArray());

        internal static IDictionary<string, string> DecodeQueryString(string queryString)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string pair in queryString.Split('&'))
            {
                string[] parts = pair.Split(new char[] { '=' }, 2);
                dict[parts[0]] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : null;
            }
            return dict;
        }

        internal static IDictionary<string, object> DeserializeJsonString(string jsonData) => Json.Parse(jsonData) as IDictionary<string, object>;

        internal static string SerializeJsonString(IDictionary<string, object> jsonData) => Json.Encode(jsonData);
    }
}
