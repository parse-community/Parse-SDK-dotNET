using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class LiveQuerySubscriptionTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true }, new LiveQueryServerConnectionData { Test = true });

    public LiveQuerySubscriptionTests() => Client.Publicize();

    [TestInitialize]
    public void SetUp()
    {
        Client.Publicize();
    }

    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestConstructor() => Assert.IsNotNull(
        new ParseLiveQuerySubscription<ParseObject>(Client.Services, "Foo", 1),
        "The subscription instance should not be null.");

    [TestMethod]
    public void TestConstructorExceptionServiceHub() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => new ParseLiveQuerySubscription<ParseObject>(null, "Foo", 1));

    [TestMethod]
    public void TestConstructorExceptionClassName() =>
        Assert.ThrowsExactly<ArgumentException>(() => new ParseLiveQuerySubscription<ParseObject>(Client.Services, String.Empty, 1));

    [TestMethod]
    public void TestCreate()
    {
        MutableObjectState state = new()
        {
            ObjectId = "waGiManPutr4Pet1r",
            ClassName = "Corgi",
            CreatedAt = new DateTime { },
            ServerData = new Dictionary<string, object>
            {
                ["key"] = "value"
            }
        };

        ParseLiveQuerySubscription<ParseObject> subscription = new(Client.Services, "Corgi", 1);

        subscription.Create += (_, args) =>
        {
            Assert.IsNotNull(args, "The event args should not be null.");
            Assert.AreEqual(args.Object.ObjectId, state.ObjectId);
            Assert.AreEqual(args.Object.ClassName, state.ClassName);
            Assert.AreEqual(args.Object["key"], state["key"]);
        };

        subscription.OnCreate(state);
    }

    [TestMethod]
    public void TestUpdate()
    {
        MutableObjectState originalState = new()
        {
            ObjectId = "waGiManPutr4Pet1r",
            ClassName = "Corgi",
            CreatedAt = new DateTime { },
            ServerData = new Dictionary<string, object>
            {
                ["key"] = "before"
            }
        };

        MutableObjectState state = originalState;
        state["key"] = "after";

        ParseLiveQuerySubscription<ParseObject> subscription = new(Client.Services, "Corgi", 1);

        subscription.Update += (_, args) =>
        {
            Assert.IsNotNull(args, "The event args should not be null.");

            Assert.AreEqual(args.Object.ObjectId, state.ObjectId);
            Assert.AreEqual(args.Object.ClassName, state.ClassName);
            Assert.AreEqual(args.Object["key"], state["key"]);

            Assert.AreEqual(args.Original.ObjectId, originalState.ObjectId);
            Assert.AreEqual(args.Original.ClassName, originalState.ClassName);
            Assert.AreEqual(args.Original["key"], originalState["key"]);
        };

        subscription.OnUpdate(state, originalState);
    }


    [TestMethod]
    public void TestEnter()
    {
        MutableObjectState originalState = new()
        {
            ObjectId = "waGiManPutr4Pet1r",
            ClassName = "Corgi",
            CreatedAt = new DateTime { },
            ServerData = new Dictionary<string, object>
            {
                ["key"] = "before"
            }
        };

        MutableObjectState state = originalState;
        state["key"] = "after";

        ParseLiveQuerySubscription<ParseObject> subscription = new(Client.Services, "Corgi", 1);

        subscription.Enter += (_, args) =>
        {
            Assert.IsNotNull(args, "The event args should not be null.");

            Assert.AreEqual(args.Object.ObjectId, state.ObjectId);
            Assert.AreEqual(args.Object.ClassName, state.ClassName);
            Assert.AreEqual(args.Object["key"], state["key"]);

            Assert.AreEqual(args.Original.ObjectId, originalState.ObjectId);
            Assert.AreEqual(args.Original.ClassName, originalState.ClassName);
            Assert.AreEqual(args.Original["key"], originalState["key"]);
        };

        subscription.OnEnter(state, originalState);
    }

    [TestMethod]
    public void TestLeave()
    {
        MutableObjectState originalState = new()
        {
            ObjectId = "waGiManPutr4Pet1r",
            ClassName = "Corgi",
            CreatedAt = new DateTime { },
            ServerData = new Dictionary<string, object>
            {
                ["key"] = "before"
            }
        };

        MutableObjectState state = originalState;
        state["key"] = "after";

        ParseLiveQuerySubscription<ParseObject> subscription = new(Client.Services, "Corgi", 1);

        subscription.Leave += (_, args) =>
        {
            Assert.IsNotNull(args, "The event args should not be null.");

            Assert.AreEqual(args.Object.ObjectId, state.ObjectId);
            Assert.AreEqual(args.Object.ClassName, state.ClassName);
            Assert.AreEqual(args.Object["key"], state["key"]);

            Assert.AreEqual(args.Original.ObjectId, originalState.ObjectId);
            Assert.AreEqual(args.Original.ClassName, originalState.ClassName);
            Assert.AreEqual(args.Original["key"], originalState["key"]);
        };

        subscription.OnLeave(state, originalState);
    }


    [TestMethod]
    public void TestDelete()
    {
        MutableObjectState state = new()
        {
            ObjectId = "waGiManPutr4Pet1r",
            ClassName = "Corgi",
            CreatedAt = new DateTime { },
            ServerData = new Dictionary<string, object>
            {
                ["key"] = "value"
            }
        };

        ParseLiveQuerySubscription<ParseObject> subscription = new(Client.Services, "Corgi", 1);

        subscription.Delete += (_, args) =>
        {
            Assert.IsNotNull(args, "The event args should not be null.");
            Assert.AreEqual(args.Object.ObjectId, state.ObjectId);
            Assert.AreEqual(args.Object.ClassName, state.ClassName);
            Assert.AreEqual(args.Object["key"], state["key"]);
        };

        subscription.OnDelete(state);
    }
}
