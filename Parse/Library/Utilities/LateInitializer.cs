using System;
using System.Collections.Generic;

namespace Parse.Library.Utilities
{
    /// <summary>
    /// A wrapper over a dictionary from value generator to value. Uses the fact that lambda expressions in a specific location are cached, so the cost of instantiating a generator delegate is only incurred once at the call site of <see cref="GetValue{TData}(Func{TData})"/> and subsequent calls look up the result of the first generation from the dictionary based on the hash of the generator delegate. This is effectively a lazy initialization mechanism that allows the member type to remain unchanged.
    /// </summary>
    public class LateInitializer
    {
        Lazy<Dictionary<Func<object>, object>> Storage { get; } = new Lazy<Dictionary<Func<object>, object>> { };

        public TData GetValue<TData>(Func<TData> generator)
        {
            lock (generator)
            {
                if (Storage.Value.TryGetValue(generator as Func<object>, out object data))
                {
                    return (TData) data;
                }
                else
                {
                    TData result = generator.Invoke();

                    Storage.Value.Add(generator as Func<object>, result);
                    return result;
                }
            }
        }
    }
}
