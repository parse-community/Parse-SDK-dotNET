#nullable enable

namespace Parse.Abstractions.Infrastructure
{
    // ALTERNATE NAME: IServiceHubRebuilder, IServiceHubConstructor, IServiceHubBuilder, IServiceHubStacker, ISericeHubCreator, IClient, IDataContainmentHub, IResourceContainmentHub, IDataContainer, IServiceHubComposer

    public interface IServiceHubBuilder
    {
        public IServiceHub BuildHub(IMutableServiceHub? serviceHub = default, IServiceHub? extension = default, params IServiceHubMutator[] mutators);
    }
}
