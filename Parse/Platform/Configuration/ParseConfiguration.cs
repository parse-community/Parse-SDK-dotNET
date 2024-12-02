using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Configuration;

/// <summary>
/// The ParseConfig is a representation of the remote configuration object,
/// that enables you to add things like feature gating, a/b testing or simple "Message of the day".
/// </summary>
public class ParseConfiguration : IJsonConvertible
{
    IDictionary<string, object> Properties { get; } = new Dictionary<string, object> { };

    IServiceHub Services { get; }

    internal ParseConfiguration(IServiceHub serviceHub) => Services = serviceHub;

    ParseConfiguration(IDictionary<string, object> properties, IServiceHub serviceHub) : this(serviceHub) => Properties = properties;

    internal static ParseConfiguration Create(IDictionary<string, object> configurationData, IParseDataDecoder decoder, IServiceHub serviceHub)
    {
        return new ParseConfiguration(decoder.Decode(configurationData["params"], serviceHub) as IDictionary<string, object>, serviceHub);
    }

    /// <summary>
    /// Gets a value for the key of a particular type.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to. Supported types are
    /// ParseObject and its descendents, Parse types such as ParseRelation and ParseGeopoint,
    /// primitive types,IList&lt;T&gt;, IDictionary&lt;string, T&gt; and strings.</typeparam>
    /// <param name="key">The key of the element to get.</param>
    /// <exception cref="KeyNotFoundException">The property is retrieved
    /// and <paramref name="key"/> is not found.</exception>
    /// <exception cref="System.FormatException">The property under this <paramref name="key"/>
    /// key was found, but of a different type.</exception>
    public T Get<T>(string key)
    {
        return Conversion.To<T>(Properties[key]);
    }

    /// <summary>
    /// Populates result with the value for the key, if possible.
    /// </summary>
    /// <typeparam name="T">The desired type for the value.</typeparam>
    /// <param name="key">The key to retrieve a value for.</param>
    /// <param name="result">The value for the given key, converted to the
    /// requested type, or null if unsuccessful.</param>
    /// <returns>true if the lookup and conversion succeeded, otherwise false.</returns>
    public bool TryGetValue<T>(string key, out T result)
    {
        if (Properties.ContainsKey(key))
            try
            {
                T temp = Conversion.To<T>(Properties[key]);
                result = temp;
                return true;
            }
            catch
            {
                // Could not convert, do nothing.
            }

        result = default;
        return false;
    }

    /// <summary>
    /// Gets a value on the config.
    /// </summary>
    /// <param name="key">The key for the parameter.</param>
    /// <exception cref="KeyNotFoundException">The property is
    /// retrieved and <paramref name="key"/> is not found.</exception>
    /// <returns>The value for the key.</returns>
    virtual public object this[string key] => Properties[key];

    IDictionary<string, object> IJsonConvertible.ConvertToJSON()
    {
        return new Dictionary<string, object>
        {
            ["params"] = NoObjectsEncoder.Instance.Encode(Properties, Services)
        };
    }
}
