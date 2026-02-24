using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse.Abstractions.Platform.LiveQueries;

namespace Parse.Platform.LiveQueries;

internal class ParseLiveQueryMessageBuilder : IParseLiveQueryMessageBuilder
{
    private async Task<IDictionary<string, object>> AppendSessionToken(IDictionary<string, object> message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        string sessionToken = await ParseClient.Instance.Services.GetCurrentSessionToken();
        if (sessionToken is not null)
        {
            Dictionary<string, object> copy = new Dictionary<string, object>(message)
            {
                { "sessionToken", sessionToken }
            };
            return copy;
        }

        return message;
    }

    public async Task<IDictionary<string, object>> BuildConnectMessage() => await AppendSessionToken(new Dictionary<string, object>
        {
            { "op", "connect" },
            {
                "applicationId",
                ParseClient.Instance.Services.LiveQueryServerConnectionData?.ApplicationID ?? throw new InvalidOperationException("LiveQueryServerConnectionData is not configured")
            },
            {
                "windowsKey",
                ParseClient.Instance.Services.LiveQueryServerConnectionData?.Key ?? throw new InvalidOperationException("LiveQueryServerConnectionData is not configured")
            }
        });

    private async Task<IDictionary<string, object>> BuildSubscriptionMessageCore<T>(string operation, int requestId, ParseLiveQuery<T> liveQuery) where T : ParseObject
    {
        if (requestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestId), "Request ID must be greater than zero.");

        if (liveQuery is null)
            throw new ArgumentNullException(nameof(liveQuery));

        return await AppendSessionToken(new Dictionary<string, object>
        {
            { "op", operation },
            { "requestId", requestId },
            { "query", liveQuery.BuildParameters() }
        });
    }

    public async Task<IDictionary<string, object>> BuildSubscribeMessage<T>(int requestId, ParseLiveQuery<T> liveQuery) where T : ParseObject
        => await BuildSubscriptionMessageCore("subscribe", requestId, liveQuery);

    public async Task<IDictionary<string, object>> BuildUpdateSubscriptionMessage<T>(int requestId, ParseLiveQuery<T> liveQuery) where T : ParseObject
        => await BuildSubscriptionMessageCore("update", requestId, liveQuery);

    public IDictionary<string, object> BuildUnsubscribeMessage(int requestId)
    {
        if (requestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestId), "Request ID must be greater than zero.");

        return new Dictionary<string, object>
        {
            { "op", "unsubscribe" },
            { "requestId", requestId }
        };
    }
}
