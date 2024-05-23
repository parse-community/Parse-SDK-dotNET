namespace Parse.Abstractions.Infrastructure.Data
{
    /// <summary>
    /// A generalized input data decoding interface for the Parse SDK.
    /// </summary>
    public interface IParseDataDecoder
    {
        /// <summary>
        /// Decodes input data into Parse-SDK-related entities, such as <see cref="ParseObject"/> instances, which is why an <see cref="IServiceHub"/> implementation instance is sometimes required.
        /// </summary>
        /// <param name="data">The target input data to decode.</param>
        /// <param name="serviceHub">A <see cref="IServiceHub"/> implementation instance to use when instantiating <see cref="ParseObject"/>s.</param>
        /// <returns>A Parse SDK entity such as a <see cref="ParseObject"/>.</returns>
        object Decode(object data, IServiceHub serviceHub);
    }
}