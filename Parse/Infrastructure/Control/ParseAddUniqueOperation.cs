using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Control
{
    public class ParseAddUniqueOperation : IParseFieldOperation
    {
        ReadOnlyCollection<object> Data { get; }

        public ParseAddUniqueOperation(IEnumerable<object> objects) => Data = new ReadOnlyCollection<object>(objects.Distinct().ToList());

        public object Encode(IServiceHub serviceHub)
        {
            return new Dictionary<string, object>
            {
                ["__op"] = "AddUnique",
                ["objects"] = PointerOrLocalIdEncoder.Instance.Encode(Data, serviceHub)
            };
        }

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
        {
            return previous switch
            {
                null => this,
                ParseDeleteOperation _ => new ParseSetOperation(Data.ToList()),
                ParseSetOperation setOp => new ParseSetOperation(Apply(Conversion.To<IList<object>>(setOp.Value), default)),
                ParseAddUniqueOperation addition => new ParseAddUniqueOperation(Apply(addition.Objects, default) as IList<object>),
                _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
            };
        }

        public object Apply(object oldValue, string key)
        {
            if (oldValue == null)
            {
                return Data.ToList();
            }

            List<object> result = Conversion.To<IList<object>>(oldValue).ToList();
            IEqualityComparer<object> comparer = ParseFieldOperations.ParseObjectComparer;

            foreach (object target in Data)
            {
                if (target is ParseObject)
                {
                    if (!(result.FirstOrDefault(reference => comparer.Equals(target, reference)) is { } matched))
                    {
                        result.Add(target);
                    }
                    else
                    {
                        result[result.IndexOf(matched)] = target;
                    }
                }
                else if (!result.Contains(target, comparer))
                {
                    result.Add(target);
                }
            }

            return result;
        }

        public IEnumerable<object> Objects => Data;
    }
}
