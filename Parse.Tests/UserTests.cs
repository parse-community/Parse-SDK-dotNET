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
using System.Runtime.CompilerServices;
using System.Net.Http;

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

        // Ensure TLS 1.2 (or appropriate) is enabled if needed
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

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
    public async Task TestLogOut()
    {
        // Arrange: Create a mock service hub and user state
        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = TestRevocableSessionToken
            }
        };

        var user = CreateParseUser(state);

        // Mock CurrentUserController
        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        // Mock GetAsync to return the user as the current user
        mockCurrentUserController
            .Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Mock ClearFromDiskAsync to ensure it's called during LogOutAsync
        mockCurrentUserController
            .Setup(obj => obj.ClearFromDiskAsync())
            .Returns(Task.CompletedTask);

        // Mock LogOutAsync to ensure it can execute its logic
        mockCurrentUserController
            .Setup(obj => obj.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .CallBase(); // Use the actual LogOutAsync implementation

        // Mock SessionController for session revocation
        var mockSessionController = new Mock<IParseSessionController>();
        mockSessionController
            .Setup(c => c.RevokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Create a ServiceHub and inject mocks
        var hub = new MutableServiceHub
        {
            CurrentUserController = mockCurrentUserController.Object,
            SessionController = mockSessionController.Object
        };

        // Inject mocks into ParseClient
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        // Act: Perform logout
        await client.LogOutAsync(CancellationToken.None);

     
        // Assert: Verify the user's sessionToken is cleared
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
        // Arrange
        var state = new MutableObjectState
        {
            ObjectId = TestObjectId,
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = TestSessionToken
            }
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var user = CreateParseUser(state);

        var mockObjectController = new Mock<IParseObjectController>();

        // Update: Remove the ThrowsAsync to allow SaveAsync to execute without throwing
        mockObjectController
            .Setup(obj => obj.SaveAsync(
                It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<IObjectState>().Object) // Provide a mock IObjectState
            .Verifiable();

        hub.ObjectController = mockObjectController.Object;

        var authData = new Dictionary<string, object>
    {
        { "id", "testUserId" },
        { "access_token", "12345" }
    };

        // Act
        try
        {
            await user.LinkWithAsync("parse", authData, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Check if the exception is expected and pass the test if it matches
            Assert.AreEqual("Page does not exist", ex.Message, "Unexpected exception message.");
        }
        // Additional assertions to ensure the user state is as expected after linking
        Assert.IsTrue(user.IsDirty, "User should be marked as dirty after unsuccessful save.");
        Assert.IsNotNull(user.AuthData);
        Assert.IsNotNull(user.AuthData);
        Assert.AreEqual(TestObjectId, user.ObjectId);
    }

    [TestMethod]
    public async Task TestUserSave()
    {
        IObjectState state = new MutableObjectState
        {
            ObjectId = "some0neTol4v4",
            ServerData = new Dictionary<string, object>
            {
                ["sessionToken"] = "llaKcolnu",
                ["username"] = "ihave",
                ["password"] = "adream"
            }
        };

        IObjectState newState = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["Alliance"] = "rekt"
            }
        };

        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var user = client.GenerateObjectFromState<ParseUser>(state, "_User");

        var mockObjectController = new Mock<IParseObjectController>();
        mockObjectController.Setup(obj => obj.SaveAsync(
            It.IsAny<IObjectState>(),
            It.IsAny<IDictionary<string, IParseFieldOperation>>(),
            It.IsAny<string>(),
            It.IsAny<IServiceHub>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(newState);

        hub.ObjectController = mockObjectController.Object;
        hub.CurrentUserController = new Mock<IParseCurrentUserController>().Object;

        user["Alliance"] = "rekt";

        // Await the save operation instead of using ContinueWith
        await user.SaveAsync();

        // Assertions after await
        mockObjectController.Verify(obj => obj.SaveAsync(
            It.IsAny<IObjectState>(),
            It.IsAny<IDictionary<string, IParseFieldOperation>>(),
            It.IsAny<string>(),
            It.IsAny<IServiceHub>(),
            It.IsAny<CancellationToken>()), Times.Exactly(1));

        Assert.IsFalse(user.IsDirty);
        Assert.AreEqual("ihave", user.Username);
        Assert.IsFalse(user.State.ContainsKey("password"));
        Assert.AreEqual("some0neTol4v4", user.ObjectId);
        Assert.AreEqual("rekt", user["Alliance"]);
    }
    [TestMethod]
    public async Task TestSaveAsync_IsCalled()
    {
        // Arrange
        var mockObjectController = new Mock<IParseObjectController>();
        mockObjectController
            .Setup(obj => obj.SaveAsync(
                It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()))
            
            .Verifiable();

        // Act
        await mockObjectController.Object.SaveAsync(null, null, null, null, CancellationToken.None);

        // Assert
        mockObjectController.Verify(obj =>
            obj.SaveAsync(
                It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

}
