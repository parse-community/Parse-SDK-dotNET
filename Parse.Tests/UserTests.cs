using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;

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
        // Arrange

        // 1. Create mocks for the specific services we need to control.
        var mockCommandRunner = new Mock<IParseCommandRunner>();
        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        // 2. Create a MUTABLE service hub and put our mocks inside.
        //    This hub has NO knowledge of the real services.
        var mockedHub = new MutableServiceHub
        {
            CommandRunner = mockCommandRunner.Object,
            CurrentUserController = mockCurrentUserController.Object
        };
        // Let the mutable hub fill in any other dependencies it needs with defaults.
        mockedHub.SetDefaults();

        // 3. Create a NEW ParseClient instance specifically for this test.
        //    We pass our MOCKED hub directly into its constructor.
        var isolatedClient = new ParseClient(new ServerConnectionData { Test = true }, mockedHub);

        // 4. Use THIS isolated client to create our user.
        //    This guarantees the user is constructed ONLY with our mocked services.
        //    It will never touch the static ParseClient.Instance.
        var user = isolatedClient.GenerateObjectFromState<ParseUser>(new MutableObjectState
        {
            ServerData = new Dictionary<string, object> { ["sessionToken"] = TestRevocableSessionToken }
        }, "_User");

        // 5. Set up the expected behavior of our mocks.
        mockCommandRunner.Setup(runner => runner.RunCommandAsync(
                It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tuple<System.Net.HttpStatusCode, IDictionary<string, object>>(System.Net.HttpStatusCode.OK, new Dictionary<string, object>()));

        mockCurrentUserController
            .Setup(c => c.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        // Call LogOutAsync on the user object that is guaranteed to be isolated.
        await user.LogOutAsync(CancellationToken.None);
        mockCommandRunner.Verify(runner => runner.RunCommandAsync(
    It.Is<ParseCommand>(cmd =>
        // Check the path
        cmd.Path.Contains("logout") &&
        // Manually check the headers
        HeadersContainSessionToken(cmd.Headers, TestRevocableSessionToken)
    ),
            It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify the local cache was told to clear.
        mockCurrentUserController.Verify(c =>
            c.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.IsNull(user.SessionToken);
    }



    private bool HeadersContainSessionToken(IEnumerable<KeyValuePair<string, string>> headers, string expectedToken)
    {
        foreach (var header in headers)
        {
            if (header.Key == "X-Parse-Session-Token" && header.Value == expectedToken)
            {
                return true; // We found it!
            }
        }
        return false; // We looped through all headers and didn't find it.
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


    //I need to test the LinkWithAsync method, but it requires a valid authData dictionary and a valid service hub setup.
    [Ignore]
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
    [TestMethod]
    [Description("Tests that SignUpAsync throws when essential properties are missing.")]
    public async Task SignUpAsync_MissingCredentials_ThrowsException()
    {
        var user = new ParseUser();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => user.SignUpAsync(), "Should throw for missing username.");

        user.Username = TestUsername;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => user.SignUpAsync(), "Should throw for missing password.");

        user.Password = TestPassword;
        user.ObjectId = TestObjectId;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => user.SignUpAsync(), "Should throw for existing ObjectId.");
    }

    //[TestMethod]
    //[Description("Tests that IsAuthenticatedAsync returns true when the user is the current user.")]
    //public async Task IsAuthenticatedAsync_WhenCurrentUserMatches_ReturnsTrue()
    //{
    //    // Arrange
    //    var mockCurrentUserController = new Mock<IParseCurrentUserController>();
    //    var hub = new MutableServiceHub { CurrentUserController = mockCurrentUserController.Object };
    //    hub.SetDefaults();
    //    var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

    //    var user = client.GenerateObjectFromState<ParseUser>(new MutableObjectState { ObjectId = TestObjectId, ServerData = new Dictionary<string, object> { ["sessionToken"] = TestSessionToken } }, "_User");

    //    // Mock GetCurrentUserAsync to return the same user.
    //    mockCurrentUserController.Setup(c => c.GetCurrentUserAsync()).ReturnsAsync(user);

    //    // Act
    //    var isAuthenticated = await user.IsAuthenticatedAsync();

    //    // Assert
    //    Assert.IsTrue(isAuthenticated);
    //}

    [TestMethod]
    [Description("Tests that IsAuthenticatedAsync returns false when there is no session token.")]
    public async Task IsAuthenticatedAsync_WhenNoSessionToken_ReturnsFalse()
    {
        // Arrange
        var user = new ParseUser { ObjectId = TestObjectId };

        // Act
        var isAuthenticated = await user.IsAuthenticatedAsync();

        // Assert
        Assert.IsFalse(isAuthenticated);
    }

    [TestMethod]
    [Description("Tests that removing the username key throws an exception.")]
    public void Remove_Username_ThrowsInvalidOperationException()
    {
        var user = new ParseUser();
        Assert.ThrowsException<InvalidOperationException>(() => user.Remove("username"));
    }

    //[TestMethod]
    //[Description("Tests that setting the session token correctly updates state and saves the current user.")]
    //public async Task SetSessionTokenAsync_SavesCurrentUser()
    //{
    //    // Arrange
    //    var mockCurrentUserController = new Mock<IParseCurrentUserController>();
    //    var hub = new MutableServiceHub { CurrentUserController = mockCurrentUserController.Object };
    //    hub.SetDefaults();
    //    var client = new ParseClient(new ServerConnectionData { Test = true }, hub);
    //    var user = client.GenerateObjectFromState<ParseUser>(new MutableObjectState(), "_User");

    //    // Act
    //    await user.SetSessionTokenAsync("new_token");

    //    // Assert
    //    Assert.AreEqual("new_token", user.SessionToken);
    //    mockCurrentUserController.Verify(c => c.SaveCurrentUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    //}

    [TestMethod]
    [Description("Tests that SaveAsync on a new user throws an exception.")]
    public async Task SaveAsync_NewUser_ThrowsInvalidOperationException()
    {
        var user = new ParseUser();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => user.SaveAsync());
    }

    //[TestMethod]
    //[Description("Tests that SaveAsync on an existing user saves the current user if they match.")]
    //public async Task SaveAsync_WhenIsCurrentUser_SavesCurrentUser()
    //{
    //    // Arrange
    //    var mockObjectController = new Mock<IParseObjectController>();
    //    var mockCurrentUserController = new Mock<IParseCurrentUserController>();
    //    var hub = new MutableServiceHub
    //    {
    //        ObjectController = mockObjectController.Object,
    //        CurrentUserController = mockCurrentUserController.Object
    //    };
    //    hub.SetDefaults();
    //    var client = new ParseClient(new ServerConnectionData { Test = true }, hub);
    //    var user = client.GenerateObjectFromState<ParseUser>(new MutableObjectState { ObjectId = TestObjectId }, "_User");

    //    mockCurrentUserController.Setup(c => c.IsCurrent(user)).Returns(true);
    //    mockObjectController.Setup(c => c.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
    //        .ReturnsAsync(user.State);

    //    // Act
    //    user.Email = "new@email.com"; // Make the user dirty
    //    await user.SaveAsync();

    //    // Assert
    //    mockCurrentUserController.Verify(c => c.SaveCurrentUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    //}

    [TestMethod]
    [Description("Tests IsLinked returns true when auth data for the provider exists.")]
    public void IsLinked_WithExistingAuthData_ReturnsTrue()
    {
        var user = new ParseUser();
        user.AuthData = new Dictionary<string, IDictionary<string, object>>
        {
            ["facebook"] = new Dictionary<string, object> { { "id", "123" } }
        };

        Assert.IsTrue(user.IsLinked("facebook"));
    }

    [TestMethod]
    [Description("Tests IsLinked returns false when auth data for the provider is null or missing.")]
    public void IsLinked_WithMissingAuthData_ReturnsFalse()
    {
        var user = new ParseUser();
        user.AuthData = new Dictionary<string, IDictionary<string, object>>
        {
            ["twitter"] = null
        };

        Assert.IsFalse(user.IsLinked("facebook"));
        Assert.IsFalse(user.IsLinked("twitter"));
    }

    [TestMethod]
    [Description("Tests that HandleSave removes the password from the server data.")]
    public void HandleSave_RemovesPasswordFromServerData()
    {
        // Arrange
        var user = new ParseUser();
        var serverState = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>
            {
                ["username"] = TestUsername,
                ["password"] = "some_hash_not_the_real_password"
            }
        };

        // Act
        user.HandleSave(serverState);

        // Assert
        Assert.IsFalse(user.State.ContainsKey("password"));
    }

}
