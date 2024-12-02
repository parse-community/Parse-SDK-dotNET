using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;

namespace Parse.Infrastructure.Control;

public class ParseSetOperation : IParseFieldOperation
{
    public ParseSetOperation(object value) => Value = value;

    public object Encode(IServiceHub serviceHub)
    {
        return PointerOrLocalIdEncoder.Instance.Encode(Value, serviceHub);
    }

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
    {
        return this;
    }

    public object Apply(object oldValue, string key)
    {
        return Value;
    }

    public object Value { get; private set; }
}
