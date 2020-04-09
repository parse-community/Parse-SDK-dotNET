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

#if DEBUG
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Parse.Test")]
#endif

namespace Parse
{
    /// <summary>
    /// ParseClient contains static functions that handle global
    /// configuration for the Parse library.
    /// </summary>
    public class ParseClient : CustomServiceHub, IServiceHubComposer
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

        /// <summary>
        /// Gets whether or not the assembly using the Parse SDK was compiled by IL2CPP.
        /// </summary>
        public static bool IL2CPPCompiled { get; set; } = AppDomain.CurrentDomain?.FriendlyName?.Equals("IL2CPP Root Domain") == true;

        /// <summary>
        /// The configured default instance of <see cref="ParseClient"/> to use.
        /// </summary>
        public static ParseClient Instance { get; private set; }

        /// <summary>
        /// The current configuration that parse has been initialized with.
        /// </summary>
        public IServerConnectionData Configuration => Services.ServerConnectionData;

        internal static string Version => typeof(ParseClient)?.Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? typeof(ParseClient)?.Assembly?.GetName()?.Version?.ToString();

        /// <summary>
        /// Services that provide essential functionality.
        /// </summary>
        public override IServiceHub Services { get; internal set; }

        // TODO: Implement IServiceHubMutator in all IServiceHub-implementing classes in Parse.Library and possibly require all implementations to do so as an efficiency improvement over instantiating an OrchestrationServiceHub, only for another one to be possibly instantiated when configurators are specified.

        /// <summary>
        /// Authenticates this client as belonging to your application. This must be
        /// called before your application can use the Parse library. The recommended
        /// way is to put a call to <c>ParseClient.Initialize</c> in your
        /// Application startup.
        /// </summary>
        /// <param name="application">The Application ID provided in the Parse dashboard.
        /// </param>
        /// <param name="serverURI">The server URI provided in the Parse dashboard.
        /// </param>
        /// <param name="serviceHub">A service hub to override internal services and thereby make the Parse SDK operate in a custom manner.</param>
        public ParseClient(string application, string serverURI, IServiceHub serviceHub = default, params IServiceHubMutator[] configurators) : this(new ServerConnectionData { ApplicationID = application, ServerURI = serverURI }, serviceHub, configurators) { }

        /// <summary>
        /// Authenticates this client as belonging to your application. This must be
        /// called before your application can use the Parse library. The recommended
        /// way is to put a call to <c>ParseClient.Initialize</c> in your
        /// Application startup.
        /// </summary>
        /// <param name="configuration">The configuration to initialize Parse with.
        /// </param>
        /// <param name="serviceHub">A service hub to override internal services and thereby make the Parse SDK operate in a custom manner.</param>
        public ParseClient(IServerConnectionData configuration, IServiceHub serviceHub = default, params IServiceHubMutator[] configurators)
        {
            Services = serviceHub is { } ? new OrchestrationServiceHub { Custom = serviceHub, Default = new ServiceHub { ServerConnectionData = GenerateServerConnectionData() } } : new ServiceHub { ServerConnectionData = GenerateServerConnectionData() } as IServiceHub;

            IServerConnectionData GenerateServerConnectionData() => configuration switch
            {
                null => throw new ArgumentNullException(nameof(configuration)),
                ServerConnectionData { Test: true } data => data,
                { ServerURI: "https://api.parse.com/1/" } => throw new InvalidOperationException("Since the official parse server has shut down, you must specify a URI that points to a hosted instance."),
                { ApplicationID: { }, ServerURI: { }, Key: { } } data => data,
                _ => throw new InvalidOperationException("The IClientConfiguration implementation instance provided to the ParseClient constructor must be populated with configuration information.")
            };

            if (configurators is { Length: int length } && length > 0)
            {
                Services = BuildHub(default, Services, configurators);
            }

            Services.ClassController.AddIntrinsic();
        }

        /// <summary>
        /// Initializes a <see cref="ParseClient"/> instance using the <see cref="IServiceHub.Cloner"/> set on the <see cref="Instance"/>'s <see cref="Services"/> <see cref="IServiceHub"/> implementation instance.
        /// </summary>
        public ParseClient() => Services = (Instance ?? throw new InvalidOperationException("A ParseClient instance with an initializer service must first be publicized in order for the default constructor to be used.")).Services.Cloner.BuildHub(Instance.Services, this);

        /// <summary>
        /// Sets this <see cref="ParseClient"/> instance as the template to create new instances from.
        /// </summary>
        ///// <param name="publicize">Declares that the current <see cref="ParseClient"/> instance should be the publicly-accesible <see cref="Instance"/>.</param>
        public void Publicize()
        {
            lock (Mutex)
            {
                Instance = this;
            }
        }

        static object Mutex { get; } = new object { };

        internal static string BuildQueryString(IDictionary<string, object> parameters) => String.Join("&", (from pair in parameters let valueString = pair.Value as string select $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(String.IsNullOrEmpty(valueString) ? Json.Encode(pair.Value) : valueString)}").ToArray());

        internal static IDictionary<string, string> DecodeQueryString(string queryString)
        {
            Dictionary<string, string> query = new Dictionary<string, string> { };

            foreach (string pair in queryString.Split('&'))
            {
                string[] parts = pair.Split(new char[] { '=' }, 2);
                query[parts[0]] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : null;
            }

            return query;
        }

        internal static IDictionary<string, object> DeserializeJsonString(string jsonData) => Json.Parse(jsonData) as IDictionary<string, object>;

        internal static string SerializeJsonString(IDictionary<string, object> jsonData) => Json.Encode(jsonData);

        public IServiceHub BuildHub(IMutableServiceHub target = default, IServiceHub extension = default, params IServiceHubMutator[] configurators)
        {
            OrchestrationServiceHub orchestrationServiceHub = new OrchestrationServiceHub { Custom = target ??= new MutableServiceHub { }, Default = extension ?? new ServiceHub { } };

            foreach (IServiceHubMutator mutator in configurators.Where(configurator => configurator.Valid))
            {
                mutator.Mutate(ref target, orchestrationServiceHub);
                orchestrationServiceHub.Custom = target;
            }

            return orchestrationServiceHub;
        }
    }
}
