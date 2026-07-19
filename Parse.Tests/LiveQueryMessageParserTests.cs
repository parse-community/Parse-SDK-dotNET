using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;

namespace Parse.Tests;

[TestClass]
public class LiveQueryMessageParserTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    public LiveQueryMessageParserTests()
    {
        Client.Publicize();
    }

    [TestMethod]
    public void TestConstructor()
    {
        ParseLiveQueryMessageParser parser = new ParseLiveQueryMessageParser(Client.Services.Decoder);

        Assert.IsNotNull(parser, "Parser should not be null after construction.");

        Assert.ThrowsExactly<ArgumentNullException>(() => new ParseLiveQueryMessageParser(null));
    }

    [TestMethod]
    public void TestGetClientId()
    {
        ParseLiveQueryMessageParser parser = new ParseLiveQueryMessageParser(Client.Services.Decoder);
        string clientId = "someClientId";

        IDictionary<string, object> message = new Dictionary<string, object> { { "clientId", clientId } };
        Assert.AreEqual(clientId, parser.GetClientId(message));

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.GetClientId(null));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetClientId(new Dictionary<string, object> { { "wrongKey", "someClientId" } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetClientId(new Dictionary<string, object> { { "clientId", 12345 } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetClientId(new Dictionary<string, object> { { "clientId", "" } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetClientId(new Dictionary<string, object> { { "clientId", "   " } }));
    }

    [TestMethod]
    public void TestGetRequestId()
    {
        ParseLiveQueryMessageParser parser = new ParseLiveQueryMessageParser(Client.Services.Decoder);
        int requestId = 42;

        IDictionary<string, object> message = new Dictionary<string, object> { { "requestId", (long) requestId } };
        Assert.AreEqual(requestId, parser.GetRequestId(message));

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.GetRequestId(null));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetRequestId(new Dictionary<string, object> { { "wrongKey", 42L } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetRequestId(new Dictionary<string, object> { { "requestId", -1L } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetRequestId(new Dictionary<string, object> { { "requestId", 0L } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetRequestId(new Dictionary<string, object> { { "requestId", "notAnInteger" } }));
    }

    [TestMethod]
    public void TestGetObjectState()
    {
        ParseLiveQueryMessageParser parser = new ParseLiveQueryMessageParser(Client.Services.Decoder);
        IDictionary<string, object> objData = new Dictionary<string, object>
        {
            { "objectId", "obj123" },
            { "className", "TestClass" },
            { "createdAt", "2023-10-01T12:00.00.000Z" },
            { "updatedAt", "2023-10-01T12:00.00.000Z" },
            {
                "ACL", new Dictionary<string, object>
                {
                    { "*", new Dictionary<string, object>{ { "read", true } } }
                }
            },
            { "foo", "bar" }
        };

        IDictionary<string, object> message = new Dictionary<string, object> { { "object", objData } };
        IObjectState state = parser.GetObjectState(message);
        Assert.IsNotNull(state);
        Assert.AreEqual("obj123", state.ObjectId);
        Assert.AreEqual("bar", state["foo"]);

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.GetObjectState(null));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetObjectState(new Dictionary<string, object> { { "wrongKey", objData } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetObjectState(new Dictionary<string, object> { { "object", null } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetObjectState(new Dictionary<string, object> { { "object", "notADictionary" } }));
    }

    [TestMethod]
    public void TestGetOriginalState()
    {
        ParseLiveQueryMessageParser parser = new ParseLiveQueryMessageParser(Client.Services.Decoder);
        IDictionary<string, object> objData = new Dictionary<string, object>
        {
            { "objectId", "obj123" },
            { "className", "TestClass" },
            { "createdAt", "2023-10-01T12:00.000Z" },
            { "updatedAt", "2023-10-01T12:00.000Z" },
            {
                "ACL", new Dictionary<string, object>
                {
                    { "*", new Dictionary<string, object>{ { "read", true } } }
                }
            },
            { "foo", "bar" }
        };
        IDictionary<string, object> message = new Dictionary<string, object> { { "original", objData } };

        IObjectState state = parser.GetOriginalState(message);
        Assert.IsNotNull(state);
        Assert.AreEqual("obj123", state.ObjectId);
        Assert.AreEqual("bar", state["foo"]);

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.GetOriginalState(null));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetOriginalState(new Dictionary<string, object> { { "wrongKey", objData } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetOriginalState(new Dictionary<string, object> { { "original", null } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetOriginalState(new Dictionary<string, object> { { "original", "notADictionary" } }));
    }

    [TestMethod]
    public void TestGetError()
    {
        ParseLiveQueryMessageParser parser = new ParseLiveQueryMessageParser(Client.Services.Decoder);
        int errorCode = 123;
        string errorMessage = "An error occurred";
        bool reconnect = true;
        IDictionary<string, object> message = new Dictionary<string, object>
        {
            { "code", (long) errorCode },
            { "error", errorMessage },
            { "reconnect", reconnect }
        };

        IParseLiveQueryMessageParser.LiveQueryError error = parser.GetError(message);
        Assert.AreEqual(3, message.Count);
        Assert.AreEqual(errorCode, error.Code);
        Assert.AreEqual(errorMessage, error.Message);
        Assert.AreEqual(reconnect, error.Reconnect);

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.GetError(null));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "wrongField", 123 } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "error", errorMessage }, { "reconnect", reconnect } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "code", "notALong" }, { "error", errorMessage }, { "reconnect", reconnect } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "code", (long) errorCode }, { "reconnect", reconnect } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "code", (long) errorCode }, { "error", 12345 }, { "reconnect", reconnect } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "code", (long) errorCode }, { "error", errorMessage } }));
        Assert.ThrowsExactly<ArgumentException>(() => parser.GetError(new Dictionary<string, object> { { "code", (long) errorCode }, { "error", errorMessage }, { "reconnect", "notABoolean" } }));
    }
}