using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;

namespace Parse.Infrastructure.Control
{
    public class ParseSetOperation : IParseFieldOperation
    {
        public ParseSetOperation(object value) => Value = value;

        public object Encode(IServiceHub serviceHub) => PointerOrLocalIdEncoder.Instance.Encode(Value, serviceHub);

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) => this;

        public object Apply(object oldValue, string key) => Value;

        public object Value { get; private set; }
    }
}
