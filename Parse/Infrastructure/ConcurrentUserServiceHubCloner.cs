using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Users;

namespace Parse.Infrastructure
{
    public class ConcurrentUserServiceHubCloner : IServiceHubCloner, IServiceHubMutator
    {
        public bool Valid { get; } = true;

        public IServiceHub BuildHub(in IServiceHub reference, IServiceHubComposer composer, params IServiceHubMutator[] requestedMutators)
        {
            return composer.BuildHub(default, reference, requestedMutators.Concat(new[] { this }).ToArray());
        }

        public void Mutate(ref IMutableServiceHub target, in IServiceHub composedHub)
        {
            target.Cloner = this;
            target.CurrentUserController = new ParseCurrentUserController(new TransientCacheController { }, composedHub.ClassController, composedHub.Decoder);
        }
    }
}
