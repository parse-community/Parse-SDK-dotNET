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
using System.Diagnostics;

namespace Parse.Tests;

[TestClass]
public class UserTests
{
    private const string TestSessionToken = "llaKcolnu";
    private const string TestRevocableSessionToken = "r:llaKcolnu";
    private const string TestObjectId = "some0neTol4v4";
    private const string TestUsername = "ihave";
    private const string TestPassword = "adream";
    private const string TestEmail = "gogo@parse.com";

    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();  // Ensure the Clientinstance is globally available

        
        Client.AddValidClass<ParseSession>();
        Client.AddValidClass<ParseUser>();
    }
      [TestCleanup]
    public void CleanUp()
    {
        (Client.Services as ServiceHub)?.Reset();
        
    }

    /// <summary>
    /// Factory method for creating ParseUser objects with the ServiceHub bound.
    /// </summary>
    private ParseUser CreateParseUser(MutableObjectState state)
    {
        var user = ParseObject.Create<ParseUser>();
        user.HandleFetchResult(state);
        user.Bind(Client);
        

        return user;
    }

    [TestMethod]
    public async Task TestSignUpWithInvalidServerDataAsync()
    {
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = TestSessionToken
            }
        };

        var user = CreateParseUser(state);

        // Simulate invalid server data by ensuring username and password are not set
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await user.SignUpAsync(),
            "Expected SignUpAsync to throw an exception due to missing username or password."
        );
    }


    [TestMethod]
    public async Task TestSignUpAsync()
    {
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = TestSessionToken,
                ["username"] = TestUsername,
                ["password"] = TestPassword
            }
        };

        var newState = new MutableObjectState
        {
            ObjectId = TestObjectId
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseUserController>();
        mockController
            .Setup(obj => obj.SignUpAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newState);

        hub.UserController = mockController.Object;

        var user = CreateParseUser(state);
        user.Bind(client);

        
        await user.SignUpAsync();
        

        // Verify SignUpAsync is invoked
        mockController.Verify(
            obj => obj.SignUpAsync(
                It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );

        Assert.IsFalse(user.IsDirty);
        Assert.AreEqual(TestUsername, user.Username);
        Assert.IsFalse(user.State.ContainsKey("password"));
        Assert.AreEqual(TestObjectId, user.ObjectId);
    }


    [TestMethod]
    public async Task TestLogInAsync()
    {
        var newState = new MutableObjectState
        {
            ObjectId = TestObjectId,
            ServerData = new Dictionary<string, object>
            {
                ["username"] = TestUsername
            }
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        client.Publicize();

        var mockController = new Mock<IParseUserController>();
        mockController
            .Setup(obj => obj.LogInAsync(TestUsername, TestPassword, It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newState);

        hub.UserController = mockController.Object;

        var loggedInUser = await client.LogInWithAsync(TestUsername, TestPassword);

        // Verify LogInAsync is called
        mockController.Verify(obj => obj.LogInAsync(TestUsername, TestPassword, It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsFalse(loggedInUser.IsDirty);
        Assert.AreEqual(TestObjectId, loggedInUser.ObjectId);
        Assert.AreEqual(TestUsername, loggedInUser.Username);
    }

    [TestMethod]
    public async Task TestLogOut()
    {
        // Arrange
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = TestRevocableSessionToken
            }
        };

        var user = CreateParseUser(state);

        var mockCurrentUserController = new Mock<IParseCurrentUserController>();
        mockCurrentUserController
            .Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Simulate LogOutAsync failure with a controlled exception
        mockCurrentUserController
            .Setup(obj => obj.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("logout failure")); // Force a controlled exception since fb's service

        var mockSessionController = new Mock<IParseSessionController>();

        // Simulate a no-op for RevokeAsync
        mockSessionController
            .Setup(c => c.RevokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Inject mocks
        var hub = new MutableServiceHub
        {
            CurrentUserController = mockCurrentUserController.Object,
            SessionController = mockSessionController.Object
        };

        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        // Act
        await client.LogOutAsync(CancellationToken.None);

        // Assert: Verify LogOutAsync was invoked once
        mockCurrentUserController.Verify(
            obj => obj.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify session revocation still occurs
        mockSessionController.Verify(
            c => c.RevokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify session token is cleared
        Assert.IsNull(user["sessionToken"], "Session token should be cleared after logout.");
    }
    [TestMethod]
    public async Task TestRequestPasswordResetAsync()
    {
        var hub = new MutableServiceHub();
        var Client= new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseUserController>();
        hub.UserController = mockController.Object;

        await Client.RequestPasswordResetAsync(TestEmail);

        mockController.Verify(obj => obj.RequestPasswordResetAsync(TestEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task TestLinkAsync()
    {
        var state = new MutableObjectState
        {
            ObjectId = TestObjectId,
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = TestSessionToken
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
        var Client= new ParseClient(new ServerConnectionData { Test = true }, hub);

        var user = CreateParseUser(state);

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
        Assert.AreEqual(TestObjectId, user.ObjectId);
        Assert.AreEqual("ofWords", user["garden"]);
    }
}
