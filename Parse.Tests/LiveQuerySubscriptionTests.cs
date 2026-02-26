using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class LiveQuerySubscriptionTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true }, new LiveQueryServerConnectionData { Test = true });

    public LiveQuerySubscriptionTests() => Client.Publicize();

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

        bool createInvoked = false;
        subscription.Create += (_, args) =>
        {
            createInvoked = true;
            Assert.IsNotNull(args, "The event args should not be null.");
            Assert.AreEqual(args.Object.ObjectId, state.ObjectId);
            Assert.AreEqual(args.Object.ClassName, state.ClassName);
            Assert.AreEqual(args.Object["key"], state["key"]);
        };

        subscription.OnCreate(state);
        Assert.IsTrue(createInvoked, "Create event should have been invoked.");
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

        MutableObjectState state = new()
        {
            ObjectId = originalState.ObjectId,
            ClassName = originalState.ClassName,
            CreatedAt = originalState.CreatedAt,
            ServerData = new Dictionary<string, object>
            {
                ["key"] = "after"
            }
        };

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

    [TestMethod]
    public async Task TestUpdateAsync_CallsController()
    {
        Mock<IParseLiveQueryController> mockController = new Mock<IParseLiveQueryController>();
        mockController
            .Setup(m => m.UpdateSubscriptionAsync(It.IsAny<ParseLiveQuery<ParseObject>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        Mock<IServiceHub> hubMock = new Mock<IServiceHub>();
        hubMock.Setup(h => h.LiveQueryController).Returns(mockController.Object);

        ParseLiveQuerySubscription<ParseObject> subscription = new ParseLiveQuerySubscription<ParseObject>(hubMock.Object, "Corgi", 42);
        ParseLiveQuery<ParseObject> query = new ParseLiveQuery<ParseObject>(hubMock.Object, "Corgi", new Dictionary<string, object>());
        CancellationTokenSource cts = new CancellationTokenSource();

        await subscription.UpdateAsync(query, cts.Token);

        mockController.Verify(m => m.UpdateSubscriptionAsync(query, 42, cts.Token), Times.Once);
    }

    [TestMethod]
    public async Task TestCancelAsync_CallsController()
    {
        Mock<IParseLiveQueryController> mockController = new Mock<IParseLiveQueryController>();
        mockController
            .Setup(m => m.UnsubscribeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        Mock<IServiceHub> hubMock = new Mock<IServiceHub>();
        hubMock.Setup(h => h.LiveQueryController).Returns(mockController.Object);

        ParseLiveQuerySubscription<ParseObject> subscription = new ParseLiveQuerySubscription<ParseObject>(hubMock.Object, "Corgi", 77);
        CancellationTokenSource cts = new CancellationTokenSource();

        await subscription.CancelAsync(cts.Token);

        mockController.Verify(m => m.UnsubscribeAsync(77, cts.Token), Times.Once);
    }

    [TestMethod]
    public void TestEvents_DoNotThrow_WhenNoHandlers()
    {
        ParseLiveQuerySubscription<ParseObject> subscription = new(Client.Services, "Corgi", 1);
        MutableObjectState state = new()
        {
            ObjectId = "id",
            ClassName = "Corgi",
            CreatedAt = new DateTime(),
            ServerData = new Dictionary<string, object> { ["k"] = "v" }
        };
        MutableObjectState original = new()
        {
            ObjectId = "id",
            ClassName = "Corgi",
            CreatedAt = new DateTime(),
            ServerData = new Dictionary<string, object> { ["k"] = "old" }
        };

        // no subscriptions attached; should not throw
        subscription.OnCreate(state);
        subscription.OnUpdate(state, original);
        subscription.OnEnter(state, original);
        subscription.OnLeave(state, original);
        subscription.OnDelete(state);
    }
}
