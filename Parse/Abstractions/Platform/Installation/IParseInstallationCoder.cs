using System.Collections.Generic;
using Parse.Abstractions.Library;

namespace Parse.Push.Internal
{
    // TODO: (richardross) once coder is refactored, make this extend IParseObjectCoder.
    public interface IParseInstallationCoder
    {
        IDictionary<string, object> Encode(ParseInstallation installation);

        ParseInstallation Decode(IDictionary<string, object> data, IServiceHub serviceHub);
    }
}