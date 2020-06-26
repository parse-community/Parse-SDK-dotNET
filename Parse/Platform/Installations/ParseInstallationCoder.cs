using System.Collections.Generic;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Data;

namespace Parse.Platform.Installations
{
    public class ParseInstallationCoder : IParseInstallationCoder
    {
        IParseDataDecoder Decoder { get; }

        IParseObjectClassController ClassController { get; }

        public ParseInstallationCoder(IParseDataDecoder decoder, IParseObjectClassController classController) => (Decoder, ClassController) = (decoder, classController);

        public IDictionary<string, object> Encode(ParseInstallation installation)
        {
            IObjectState state = installation.State;
            IDictionary<string, object> data = PointerOrLocalIdEncoder.Instance.Encode(state.ToDictionary(pair => pair.Key, pair => pair.Value), installation.Services) as IDictionary<string, object>;

            data["objectId"] = state.ObjectId;

            // The following operations use the date and time serialization format defined by ISO standard 8601.

            if (state.CreatedAt is { })
                data["createdAt"] = state.CreatedAt.Value.ToString(ParseClient.DateFormatStrings[0]);

            if (state.UpdatedAt is { })
                data["updatedAt"] = state.UpdatedAt.Value.ToString(ParseClient.DateFormatStrings[0]);

            return data;
        }

        public ParseInstallation Decode(IDictionary<string, object> data, IServiceHub serviceHub) => ClassController.GenerateObjectFromState<ParseInstallation>(ParseObjectCoder.Instance.Decode(data, Decoder, serviceHub), "_Installation", serviceHub);
    }
}