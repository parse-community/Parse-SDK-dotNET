using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.Installations;

// TODO: (richardross) once coder is refactored, make this extend IParseObjectCoder.

public interface IParseInstallationCoder
{
    IDictionary<string, object> Encode(ParseInstallation installation);

    ParseInstallation Decode(IDictionary<string, object> data, IServiceHub serviceHub);
}