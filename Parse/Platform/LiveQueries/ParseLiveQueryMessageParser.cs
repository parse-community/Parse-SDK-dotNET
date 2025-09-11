using System;
using System.Collections.Generic;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Data;

namespace Parse.Platform.LiveQueries;

class ParseLiveQueryMessageParser : IParseLiveQueryMessageParser
{
    private IParseDataDecoder Decoder { get; }

    public ParseLiveQueryMessageParser(IParseDataDecoder decoder)
    {
        Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
    }

    public string GetClientId(IDictionary<string, object> message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (!(message.TryGetValue("clientId", out object clientIdObj) && clientIdObj is string clientId && !String.IsNullOrWhiteSpace(clientId)))
            throw new ArgumentException(@"Message does not contain a valid client ID.", nameof(message));

        return clientId;
    }

    public int GetRequestId(IDictionary<string, object> message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (!(message.TryGetValue("requestId", out object requestIdObj) && requestIdObj is long requestIdLong && requestIdLong > 0))
            throw new ArgumentException(@"Message does not contain a valid request ID.", nameof(message));

        return (int)requestIdLong;
    }

    private IDictionary<string, object> GetDictionary(IDictionary<string, object> message, string key)
    {
        if (!(message.TryGetValue(key, out object obj) && obj is IDictionary<string, object> dict))
            throw new ArgumentException(@"Message does not contain a valid %{key}.", nameof(message));

        return dict;
    }

    public IObjectState GetObjectState(IDictionary<string, object> message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        IDictionary<string, object> current = GetDictionary(message, "object")
            ?? throw new ArgumentException("Message does not contain a valid object state.", nameof(message));

        return ParseObjectCoder.Instance.Decode(current, Decoder, ParseClient.Instance.Services);
    }

    public IObjectState GetOriginalState(IDictionary<string, object> message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        IDictionary<string, object> original = GetDictionary(message, "original")
            ?? throw new ArgumentException("Message does not contain a valid original object state.", nameof(message));

        return ParseObjectCoder.Instance.Decode(original, Decoder, ParseClient.Instance.Services);
    }

    public (int code, string error, bool reconnect) GetError(IDictionary<string, object> message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (!(message.TryGetValue("code", out object codeObj) && codeObj is long codeLong))
            throw new ArgumentException("Message does not contain a valid code.", nameof(message));

        if (!(message.TryGetValue("error", out object errorObj) && errorObj is string error))
            throw new ArgumentException("Message does not contain a valid error description.", nameof(message));

        if (!(message.TryGetValue("reconnect", out object reconnectObj) && reconnectObj is bool reconnect))
            throw new ArgumentException("Message does not contain a valid reconnect flag.", nameof(message));

        return ((int)codeLong, error, reconnect);
    }
}