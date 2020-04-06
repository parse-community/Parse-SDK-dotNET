using System;
using System.Collections.Generic;
using System.Text;
using Parse.Abstractions.Library;

namespace Parse.Library
{
    public class ConcurrentUserServiceHubCloner : IServiceHubCloner
    {
        public IServiceHub BuildHub(in IServiceHub reference, IServiceHubComposer composer)
        {
            throw new NotImplementedException { };
        }
    }
}
