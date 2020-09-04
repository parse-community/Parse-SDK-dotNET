namespace Parse.Abstractions.Infrastructure
{
    /// <summary>
    /// Generates an <see cref="IServiceHub"/> as a clone of another. This clone could be mutated.
    /// </summary>
    public interface IServiceHubCloner
    {
        /// <summary>
        /// Clones <paramref name="hub"/>, via the <paramref name="builder"/> if needed, and should execute the <paramref name="mutators"/>. The clone could be observed in a mutated state versus <paramref name="hub"/>.
        /// </summary>
        /// <param name="hub">The hub to use as a reference or combination extension to new hub.</param>
        /// <param name="builder">The builder which could be used to create the clone.</param>
        /// <param name="mutators">Additional mutators to execute on the cloned hub.</param>
        /// <returns>A service hub which could be both based on <paramref name="hub"/>, and observed in a mutated state versus <paramref name="hub"/>.</returns>
        public IServiceHub CloneHub(in IServiceHub hub, IServiceHubBuilder builder, params IServiceHubMutator[] mutators);
    }
}
