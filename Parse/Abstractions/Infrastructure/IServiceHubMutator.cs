namespace Parse.Abstractions.Infrastructure
{
    // IServiceHubComposer, IServiceHubMutator, IServiceHubConfigurator, IClientConfigurator, IServiceConfigurationLayer

    /// <summary>
    /// A class which makes a deliberate mutation to a service.
    /// </summary>
    public interface IServiceHubMutator
    {
        bool Valid { get; }

        void Mutate(ref IMutableServiceHub target, in IServiceHub composedHub);
    }
}
