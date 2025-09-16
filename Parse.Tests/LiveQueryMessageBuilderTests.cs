using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;

namespace Parse.Tests;

[TestClass]
public class LiveQueryMessageBuilderTests
{
    private readonly Mock<IParseCurrentUserController> _mockCurrentUserController;
    private readonly MutableServiceHub _hub;

    private ParseClient Client { get; }

    public LiveQueryMessageBuilderTests()
    {
        _mockCurrentUserController = new Mock<IParseCurrentUserController>();

        _mockCurrentUserController
            .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3ss!0nT0k3n");

        _hub = new MutableServiceHub { CurrentUserController = _mockCurrentUserController.Object };

        Client = new ParseClient(
            new ServerConnectionData { Test = true },
            new LiveQueryServerConnectionData { ApplicationID = "TestApp", Key = "t3stK3y", Test = true },
            _hub);

        Client.Publicize();
    }

    [TestMethod]
    public async Task TestBuildConnectMessage()
    {
        ParseLiveQueryMessageBuilder builder = new ParseLiveQueryMessageBuilder();
        IDictionary<string, object> message = await builder.BuildConnectMessage();

        Assert.IsNotNull(message);
        Assert.IsTrue(message.ContainsKey("op"));
        Assert.IsTrue(message.ContainsKey("applicationId"));
        Assert.IsTrue(message.ContainsKey("windowsKey"));
        Assert.IsTrue(message.ContainsKey("sessionToken"));
        Assert.HasCount(4, message);
        Assert.AreEqual("connect", message["op"]);
        Assert.AreEqual(Client.Services.LiveQueryServerConnectionData.ApplicationID, message["applicationId"]);
        Assert.AreEqual(Client.Services.LiveQueryServerConnectionData.Key, message["windowsKey"]);
        Assert.AreEqual(await Client.Services.GetCurrentSessionToken(), message["sessionToken"]);
    }

    [TestMethod]
    public void TestBuildUnsubscribeMessage()
    {
        int requestId = 2;
        ParseLiveQueryMessageBuilder builder = new ParseLiveQueryMessageBuilder();
        IDictionary<string, object> message = builder.BuildUnsubscribeMessage(requestId);

        Assert.IsNotNull(message);
        Assert.IsTrue(message.ContainsKey("op"));
        Assert.IsTrue(message.ContainsKey("requestId"));
        Assert.HasCount(2, message);
        Assert.AreEqual("unsubscribe", message["op"]);
        Assert.AreEqual(requestId, message["requestId"]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.BuildUnsubscribeMessage(0));
    }

    private void ValidateSubscriptionMessage(IDictionary<string, object> message, string expectedOp, int requestId)
    {
        Assert.IsNotNull(message);
        Assert.HasCount(4, message);

        Assert.IsTrue(message.ContainsKey("op"));
        Assert.AreEqual(expectedOp, message["op"]);

        Assert.IsTrue(message.ContainsKey("requestId"));
        Assert.AreEqual(requestId, message["requestId"]);

        Assert.IsTrue(message.ContainsKey("query"));
        Assert.IsInstanceOfType<IDictionary<string, object>>(message["query"], "The 'query' value should be a Dictionary<string, object>.");
        Assert.HasCount(4, (IDictionary<string, object>) message["query"]);
        IDictionary<string, object> query = message["query"] as IDictionary<string, object>;

        Assert.IsTrue(query.ContainsKey("className"), "The 'query' dictionary should contain the 'className' key.");
        Assert.AreEqual("DummyClass", query["className"], "The 'className' property should be 'DummyClass'.");

        Assert.IsTrue(query.ContainsKey("where"), "The 'query' dictionary should contain the 'where' key.");
        Assert.IsInstanceOfType<IDictionary<string, object>>(query["where"], "The 'where' property should be a Dictionary<string, object>.");
        IDictionary<string, object> where = (IDictionary<string, object>) query["where"];
        Assert.HasCount(1, where, "The 'where' dictionary should contain exactly one key-value pair.");
        Assert.IsTrue(where.ContainsKey("foo"), "The 'where' dictionary should contain the 'foo' key.");
        Assert.AreEqual("bar", where["foo"], "The 'foo' property in 'where' should be 'bar'.");

        Assert.IsTrue(query.ContainsKey("keys"), "The 'query' dictionary should contain the 'keys' key.");
        Assert.IsInstanceOfType<string[]>(query["keys"], "The 'keys' property should be a string array.");
        Assert.HasCount(1, (string[]) query["keys"], "The 'keys' array should contain exactly one element.");
        Assert.AreEqual("foo", ((string[]) query["keys"])[0], "The 'keys' parameter should contain 'foo'.");

        Assert.IsTrue(query.ContainsKey("watch"), "The 'query' dictionary should contain the 'watch' key.");
        Assert.IsInstanceOfType<string[]>(query["watch"], "The 'watch' property should be a string array.");
        Assert.HasCount(1, (string[]) query["watch"], "The 'watch' array should contain exactly one element.");
        Assert.AreEqual("foo", ((string[]) query["watch"])[0], "The 'watch' parameter should contain 'foo'.");

    }

    [TestMethod]
    public async Task TestBuildSubscribeMessage()
    {
        int requestId = 2;
        ParseLiveQuery<ParseObject> liveQuery = new ParseLiveQuery<ParseObject>(
            Client.Services,
            "DummyClass",
            new Dictionary<string, object> { { "foo", "bar" } },
            ["foo"],
            ["foo"]);
        ParseLiveQueryMessageBuilder builder = new ParseLiveQueryMessageBuilder();
        IDictionary<string, object> message = await builder.BuildSubscribeMessage<ParseObject>(requestId, liveQuery);

        ValidateSubscriptionMessage(message, "subscribe", requestId);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await builder.BuildSubscribeMessage<ParseObject>(0, liveQuery));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await builder.BuildSubscribeMessage<ParseObject>(requestId, null));
    }

    [TestMethod]
    public async Task TestBuildUpdateSubscriptionMessage()
    {
        int requestId = 2;
        ParseLiveQuery<ParseObject> liveQuery = new ParseLiveQuery<ParseObject>(
            Client.Services,
            "DummyClass",
            new Dictionary<string, object> { { "foo", "bar" } },
            ["foo"],
            ["foo"]);
        ParseLiveQueryMessageBuilder builder = new ParseLiveQueryMessageBuilder();
        IDictionary<string, object> message = await builder.BuildUpdateSubscriptionMessage<ParseObject>(requestId, liveQuery);

        ValidateSubscriptionMessage(message, "update", requestId);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await builder.BuildUpdateSubscriptionMessage<ParseObject>(0, liveQuery));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await builder.BuildUpdateSubscriptionMessage<ParseObject>(requestId, null));
    }
}