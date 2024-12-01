using System;
using System.Collections.Generic;
using Parse.Abstractions.Infrastructure.Control;

namespace Parse.Infrastructure.Control
{
    public class ParseObjectIdComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object p1, object p2)
        {
            ParseObject parseObj1 = p1 as ParseObject;
            ParseObject parseObj2 = p2 as ParseObject;
            if (parseObj1 != null && parseObj2 != null)
            {
                return Equals(parseObj1.ObjectId, parseObj2.ObjectId);
            }
            return Equals(p1, p2);
        }

        public int GetHashCode(object p)
        {
            ParseObject parseObject = p as ParseObject;
            if (parseObject != null)
            {
                return parseObject.ObjectId.GetHashCode();
            }
            return p.GetHashCode();
        }
    }

    static class ParseFieldOperations
    {
        private static ParseObjectIdComparer comparer;

        public static IParseFieldOperation Decode(IDictionary<string, object> json)
        {
            throw new NotImplementedException();
        }

        public static IEqualityComparer<object> ParseObjectComparer
        {
            get
            {
                if (comparer == null)
                {
                    comparer = new ParseObjectIdComparer();
                }
                return comparer;
            }
        }
    }
}
