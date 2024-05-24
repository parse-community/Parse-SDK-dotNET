using System.Collections.Generic;

namespace Parse.Abstractions.Infrastructure
{
    // IServiceHubComposer, IServiceHubMutator, IServiceHubConfigurator, IClientConfigurator, IServiceConfigurationLayer

    /// <summary>
    /// A definition of a deliberate mutation to a service hub.
    /// </summary>
    public interface IServiceHubMutator
    {
        /// <summary>
        /// A value which dictates whether or not the <see cref="IServiceHubMutator"/> should be considered in a valid state.
        /// </summary>
        bool Valid { get; }

        //IServiceHubMutator[] Mutators => new[] { this };

        // customHub, mutableHub, slice, target
        // combinedHub, stackedHub, overallHub, resultantHub, composedHub

        /// <summary>
        /// A method which mutates an <see cref="IMutableServiceHub"/> implementation instance.
        /// </summary>
        /// <param name="mutableHub">The target <see cref="IMutableServiceHub"/> implementation instance</param>
        /// <param name="consumableHub">A hub which the <paramref name="mutableHub"/> is combined onto that should be used when <see cref="Mutate(ref IMutableServiceHub, in IServiceHub)"/> needs to access services.</param>
        /// <param name="futureMutators">The mutators that will be executed after this one.</param>
        void Mutate(ref IMutableServiceHub mutableHub, in IServiceHub consumableHub, Stack<IServiceHubMutator> futureMutators);
    }
}
