using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class UserTests
{
    private ParseClient Client { get; set; } = new ParseClient(new ServerConnectionData { Test = true });

    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public async Task TestSignUpWithInvalidServerDataAsync()
    {
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = "llaKcolnu"
            }
        };

        var user = Client.GenerateObjectFromState<ParseUser>(state, "_User");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await user.SignUpAsync());
    }

    [TestMethod]
    public async Task TestSignUpAsync()
    {
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = "llaKcolnu",
                ["username"] = "ihave",
                ["password"] = "adream"
            }
        };

        var newState = new MutableObjectState
        {
            ObjectId = "some0neTol4v4"
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var user = client.GenerateObjectFromState<ParseUser>(state, "_User");

        var mockController = new Mock<IParseUserController>();
        mockController
            .Setup(obj => obj.SignUpAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newState);

        hub.UserController = mockController.Object;

        await user.SignUpAsync();

        mockController.Verify(obj => obj.SignUpAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsFalse(user.IsDirty);
        Assert.AreEqual("ihave", user.Username);
        Assert.IsFalse(user.State.ContainsKey("password"));
        Assert.AreEqual("some0neTol4v4", user.ObjectId);
    }

    [TestMethod]
    public async Task TestLogInAsync()
    {
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = "llaKcolnu",
                ["username"] = "ihave",
                ["password"] = "adream"
            }
        };

        var newState = new MutableObjectState
        {
            ObjectId = "some0neTol4v4"
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseUserController>();
        mockController
            .Setup(obj => obj.LogInAsync("ihave", "adream", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newState);

        hub.UserController = mockController.Object;

        var user = await client.LogInAsync("ihave", "adream");

        mockController.Verify(obj => obj.LogInAsync("ihave", "adream", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsFalse(user.IsDirty);
        Assert.IsNull(user.Username);
        Assert.AreEqual("some0neTol4v4", user.ObjectId);
    }

    [TestMethod]
    public async Task TestLogOutAsync()
    {
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = "r:llaKcolnu"
            }
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var user = client.GenerateObjectFromState<ParseUser>(state, "_User");

        var mockCurrentUserController = new Mock<IParseCurrentUserController>();
        mockCurrentUserController
            .Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var mockSessionController = new Mock<IParseSessionController>();
        mockSessionController.Setup(c => c.IsRevocableSessionToken(It.IsAny<string>())).Returns(true);

        hub.CurrentUserController = mockCurrentUserController.Object;
        hub.SessionController = mockSessionController.Object;

        await client.LogOutAsync();

        mockCurrentUserController.Verify(obj => obj.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Once);
        mockSessionController.Verify(obj => obj.RevokeAsync("r:llaKcolnu", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task TestRequestPasswordResetAsync()
    {
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseUserController>();
        hub.UserController = mockController.Object;

        await client.RequestPasswordResetAsync("gogo@parse.com");

        mockController.Verify(obj => obj.RequestPasswordResetAsync("gogo@parse.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task TestLinkAsync()
    {
        var state = new MutableObjectState
        {
            ObjectId = "some0neTol4v4",
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = "llaKcolnu"
            }
        };

        var newState = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["garden"] = "ofWords"
            }
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var user = client.GenerateObjectFromState<ParseUser>(state, "_User");

        var mockObjectController = new Mock<IParseObjectController>();
        mockObjectController
            .Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newState);

        hub.ObjectController = mockObjectController.Object;

        await user.LinkWithAsync("parse", new Dictionary<string, object>(), CancellationToken.None);

        mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsFalse(user.IsDirty);
        Assert.IsNotNull(user.AuthData);
        Assert.IsNotNull(user.AuthData["parse"]);
        Assert.AreEqual("some0neTol4v4", user.ObjectId);
        Assert.AreEqual("ofWords", user["garden"]);
    }
}
