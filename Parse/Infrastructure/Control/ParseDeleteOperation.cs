using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;

namespace Parse.Infrastructure.Control
{
    /// <summary>
    /// An operation where a field is deleted from the object.
    /// </summary>
    public class ParseDeleteOperation : IParseFieldOperation
    {
        internal static object Token { get; } = new object { };

        public static ParseDeleteOperation Instance { get; } = new ParseDeleteOperation { };

        private ParseDeleteOperation() { }

        public object Encode(IServiceHub serviceHub)
        {
            return new Dictionary<string, object> { ["__op"] = "Delete" };
        }

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
        {
            return this;
        }

        public object Apply(object oldValue, string key)
        {
            return Token;
        }
    }
}
