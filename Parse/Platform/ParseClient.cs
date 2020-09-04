// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure;

#if DEBUG
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Parse.Tests")]
#endif

namespace Parse
{
    /// <summary>
    /// ParseClient contains static functions that handle global
    /// configuration for the Parse library.
    /// </summary>
    public class ParseClient : CustomServiceHub, IServiceHubBuilder
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

        internal static string Version => typeof(ParseClient)?.Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? typeof(ParseClient)?.Assembly?.GetName()?.Version?.ToString();

        /// <summary>
        /// Services that provide essential functionality.
        /// </summary>
        public override IServiceHub Services { get; internal set; }

        // TODO: Implement IServiceHubMutator in all IServiceHub-implementing classes in Parse.Library and possibly require all implementations to do so as an efficiency improvement over instantiating an OrchestrationServiceHub, only for another one to be possibly instantiated when configurators are specified.

        /// <summary>
        /// Creates a new <see cref="ParseClient"/> and authenticates it as belonging to your application. This class is a hub for interacting with the SDK. The recommended way to use this class on client applications is to instantiate it, then call <see cref="Publicize"/> on it in your application entry point. This allows you to access <see cref="Instance"/>.
        /// </summary>
        /// <param name="applicationID">The Application ID provided in the Parse dashboard.</param>
        /// <param name="serverURI">The server URI provided in the Parse dashboard.</param>
        /// <param name="key">The .NET Key provided in the Parse dashboard.</param>
        /// <param name="serviceHub">A service hub to override internal services and thereby make the Parse SDK operate in a custom manner.</param>
        /// <param name="mutators">A set of <see cref="IServiceHubMutator"/> implementation instances to tweak the behaviour of the SDK.</param>
        public ParseClient(string applicationID, string serverURI, string key, IServiceHub serviceHub = default, params IServiceHubMutator[] mutators) : this(new ServerConnectionData { ApplicationID = applicationID, ServerURI = serverURI, Key = key }, serviceHub, mutators) { }

        /// <summary>
        /// Creates a new <see cref="ParseClient"/> and authenticates it as belonging to your application. This class is a hub for interacting with the SDK. The recommended way to use this class on client applications is to instantiate it, then call <see cref="Publicize"/> on it in your application entry point. This allows you to access <see cref="Instance"/>.
        /// </summary>
        /// <param name="serverConnectionData">The configuration to initialize Parse with.</param>
        /// <param name="serviceHub">A service hub to override internal services and thereby make the Parse SDK operate in a custom manner.</param>
        /// <param name="mutators">A set of <see cref="IServiceHubMutator"/> implementation instances to tweak the behaviour of the SDK.</param>
        public ParseClient(IServerConnectionData serverConnectionData, IServiceHub serviceHub = default, params IServiceHubMutator[] mutators)
        {
            Services = serviceHub is { } ? new OrchestrationServiceHub { Custom = serviceHub, Default = new ServiceHub { ServerConnectionData = GenerateServerConnectionData() } } : new ServiceHub { ServerConnectionData = GenerateServerConnectionData() } as IServiceHub;

            IServerConnectionData GenerateServerConnectionData() => serverConnectionData switch
            {
                null => throw new ArgumentNullException(nameof(serverConnectionData)),
                ServerConnectionData { Test: true, ServerURI: { } } data => data,
                ServerConnectionData { Test: true } data => new ServerConnectionData
                {
                    ApplicationID = data.ApplicationID,
                    Headers = data.Headers,
                    MasterKey = data.MasterKey,
                    Test = data.Test,
                    Key = data.Key,
                    ServerURI = "https://api.parse.com/1/"
                },
                { ServerURI: "https://api.parse.com/1/" } => throw new InvalidOperationException("Since the official Parse server has shut down, you must specify a URI that points to a hosted instance."),
                { ApplicationID: { }, ServerURI: { }, Key: { } } data => data,
                _ => throw new InvalidOperationException("The IServerConnectionData implementation instance provided to the ParseClient constructor must be populated with the information needed to connect to a Parse server instance.")
            };

            if (mutators is { Length: int length } && length > 0)
            {
                Services = serviceHub switch
                {
                    IMutableServiceHub { } mutableServiceHub => BuildHub((Hub: mutableServiceHub, mutableServiceHub.ServerConnectionData = serviceHub.ServerConnectionData ?? Services.ServerConnectionData).Hub, Services, mutators),
                    { } => BuildHub(default, Services, mutators)
                };
            }

            Services.ClassController.AddIntrinsic();
        }

        /// <summary>
        /// Initializes a <see cref="ParseClient"/> instance using the <see cref="IServiceHub.Cloner"/> set on the <see cref="Instance"/>'s <see cref="Services"/> <see cref="IServiceHub"/> implementation instance.
        /// </summary>
        public ParseClient() => Services = (Instance ?? throw new InvalidOperationException("A ParseClient instance with an initializer service must first be publicized in order for the default constructor to be used.")).Services.Cloner.CloneHub(Instance.Services, this);

        /// <summary>
        /// Sets this <see cref="ParseClient"/> instance as the template to create new instances from.
        /// </summary>
        public void Publicize()
        {
            lock (Mutex)
            {
                Instance = this;
            }
        }

        static object Mutex { get; } = new object { };

        public IServiceHub BuildHub(IMutableServiceHub baseHub = default, IServiceHub extension = default, params IServiceHubMutator[] mutators)
        {
            OrchestrationServiceHub orchestrationServiceHub = new OrchestrationServiceHub { Custom = baseHub ??= new MutableServiceHub { }, Default = extension ?? new ServiceHub { } };

            Stack<IServiceHubMutator> validMutators = new Stack<IServiceHubMutator>(mutators.Where(mutator => mutator.Valid).Reverse());
            while (validMutators.Count > 0 && GetMutator() is { } mutator)
            {
                if (mutator is { Valid: true })
                {
                    mutator.Mutate(ref baseHub, orchestrationServiceHub, validMutators);
                    orchestrationServiceHub.Custom = baseHub;
                }
            }

            return orchestrationServiceHub;
            IServiceHubMutator GetMutator() => validMutators.Pop();
        }
    }
}
