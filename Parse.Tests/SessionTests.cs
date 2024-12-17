using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Sessions;
using Parse.Infrastructure;
using Parse;
using Parse.Platform.Objects;
using Parse.Abstractions.Platform.Users;

[TestClass]
public class SessionTests
{
    private ParseClient Client { get; }

    public SessionTests()
    {
        // Initialize the client
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();  // Ensure the client instance is globally available

        // Register the valid classes
        Client.AddValidClass<ParseSession>();
        Client.AddValidClass<ParseUser>();
    }

    [TestInitialize]
    public void SetUp()
    {
        // Additional setup can go here
    }

    [TestCleanup]
    public void TearDown()
    {
        // Reset any state if necessary
        (Client.Services as ServiceHub)?.Reset();
    }

    [TestMethod]
    public void TestGetSessionQuery()
    {
        // Test that GetSessionQuery returns the correct type
        Assert.IsInstanceOfType(Client.GetSessionQuery(), typeof(ParseQuery<ParseSession>));
    }

    [TestMethod]
    public void TestGetSessionToken()
    {
        var session = Client.GenerateObjectFromState<ParseSession>(
            new MutableObjectState
            {
                ServerData = new Dictionary<string, object> { ["sessionToken"] = "llaKcolnu" }
            },
            "_Session"
        );

        Assert.IsNotNull(session);
        Assert.AreEqual("llaKcolnu", session.SessionToken);
    }

    [TestMethod]
    public async Task TestGetCurrentSessionAsync()
    {
        // Set up the service hub and mock controllers
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var sessionState = new MutableObjectState
        {
            ServerData = new Dictionary<string, object> { ["sessionToken"] = "newllaKcolnu" }
        };

        // Mock session controller
        var mockController = new Mock<IParseSessionController>();
        mockController
            .Setup(obj => obj.GetSessionAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionState);

        // Mock user controller
        var userState = new MutableObjectState
        {
            ServerData = new Dictionary<string, object> { ["sessionToken"] = "llaKcolnu" }
        };

        var user = client.GenerateObjectFromState<ParseUser>(userState, "_User");

        var mockCurrentUserController = new Mock<IParseCurrentUserController>();
        mockCurrentUserController
            .Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        hub.SessionController = mockController.Object;
        hub.CurrentUserController = mockCurrentUserController.Object;

        var session = await client.GetCurrentSessionAsync();

        // Assertions
        Assert.IsNotNull(session);
        Assert.AreEqual("newllaKcolnu", session.SessionToken);

        // Verify that the session controller was called with the correct session token
        mockController.Verify(
            obj => obj.GetSessionAsync("llaKcolnu", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task TestGetCurrentSessionWithNoCurrentUserAsync()
    {
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseSessionController>();
        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        hub.SessionController = mockController.Object;
        hub.CurrentUserController = mockCurrentUserController.Object;

        var session = await client.GetCurrentSessionAsync();

        // Assertions
        Assert.IsNull(session);
    }

    [TestMethod]
    public async Task TestRevokeAsync()
    {
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseSessionController>();
        mockController.Setup(sessionController => sessionController.IsRevocableSessionToken(It.IsAny<string>())).Returns(true);

        hub.SessionController = mockController.Object;

        using var cancellationTokenSource = new CancellationTokenSource();
        await client.RevokeSessionAsync("r:someSession", cancellationTokenSource.Token);

        // Assertions
        mockController.Verify(
            obj => obj.RevokeAsync("r:someSession", cancellationTokenSource.Token),
            Times.Once
        );
    }

    [TestMethod]
    public async Task TestUpgradeToRevocableSessionAsync()
    {
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object> { ["sessionToken"] = "llaKcolnu" }
        };

        var mockController = new Mock<IParseSessionController>();
        mockController
            .Setup(obj => obj.UpgradeToRevocableSessionAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        hub.SessionController = mockController.Object;

        using var cancellationTokenSource = new CancellationTokenSource();
        var sessionToken = await client.UpgradeToRevocableSessionAsync("someSession", cancellationTokenSource.Token);

        // Assertions
        Assert.AreEqual("llaKcolnu", sessionToken);

        mockController.Verify(
            obj => obj.UpgradeToRevocableSessionAsync("someSession", It.IsAny<IServiceHub>(), cancellationTokenSource.Token),
            Times.Once
        );
    }
}
