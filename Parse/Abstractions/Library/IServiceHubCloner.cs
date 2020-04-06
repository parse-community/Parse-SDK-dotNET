using System;
using System.Text;

namespace Parse.Abstractions.Library
{
    public interface IServiceHubCloner
    {
        public IServiceHub BuildHub(in IServiceHub reference, IServiceHubComposer composer);
    }
}
