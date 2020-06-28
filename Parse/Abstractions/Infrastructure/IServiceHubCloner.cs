namespace Parse.Abstractions.Infrastructure
{
    public interface IServiceHubCloner
    {
        public IServiceHub BuildHub(in IServiceHub reference, IServiceHubComposer composer, params IServiceHubMutator[] requestedMutators);
    }
}
