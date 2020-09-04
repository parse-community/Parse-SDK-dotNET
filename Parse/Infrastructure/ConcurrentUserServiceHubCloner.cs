using System.Collections.Generic;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Users;

namespace Parse.Infrastructure
{
    /// <summary>
    /// Makes it so that <see cref="ParseClient"/> clones can be made each with a reset <see cref="IServiceHub.CurrentUserController"/>, but otherwise identical services unless modified.
    /// </summary>
    public class ConcurrentUserServiceHubCloner : IServiceHubCloner, IServiceHubMutator
    {
        /// <inheritdoc/>
        public bool Valid { get; } = true;

        /// <summary>
        /// Mutators which need to be executed on each new hub for a user.
        /// </summary>
        public List<IServiceHubMutator> BoundMutators { get; set; } = new List<IServiceHubMutator> { };

        /// <inheritdoc/>
        public IServiceHub CloneHub(in IServiceHub hub, IServiceHubBuilder builder, params IServiceHubMutator[] mutators) => builder.BuildHub(default, hub, mutators.Concat(new[] { this }).ToArray());

        /// <inheritdoc/>
        public void Mutate(ref IMutableServiceHub mutableHub, in IServiceHub consumableHub, Stack<IServiceHubMutator> futureMutators)
        {
            mutableHub.Cloner = this;
            mutableHub.CurrentUserController = new ParseCurrentUserController(new TransientCacheController { }, consumableHub.ClassController, consumableHub.Decoder);

            BoundMutators.ForEach(mutator => futureMutators.Push(mutator));
        }
    }
}
