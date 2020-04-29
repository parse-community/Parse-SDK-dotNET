using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parse.Abstractions.Library;
using Parse.Common.Internal;
using Parse.Core.Internal;

namespace Parse.Library
{
    public class ConcurrentUserServiceHubCloner : IServiceHubCloner, IServiceHubMutator
    {
        public bool Valid { get; } = true;

        public IServiceHub BuildHub(in IServiceHub reference, IServiceHubComposer composer, params IServiceHubMutator[] requestedMutators) => composer.BuildHub(default, reference, requestedMutators.Concat(new[] { this }).ToArray());

        public void Mutate(ref IMutableServiceHub target, in IServiceHub composedHub)
        {
            target.Cloner = this;
            target.CurrentUserController = new ParseCurrentUserController(new ConcurrentUserStorageController { }, composedHub.ClassController, composedHub.Decoder);
        }
    }
}
