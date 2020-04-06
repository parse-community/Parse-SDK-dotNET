namespace Parse.Abstractions.Library
{
    // ALTERNATE NAME: IClient, IDataContainmentHub, IResourceContainmentHub, IDataContainer, IServiceHubComposer

    public interface IServiceHubComposer
    {
        public IServiceHub BuildHub(IMutableServiceHub serviceHub = default, IServiceHub extension = default, params IServiceHubMutator[] configurators);
    }
}
