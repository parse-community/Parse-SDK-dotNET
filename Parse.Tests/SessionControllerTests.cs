using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Sessions;

namespace Parse.Tests;

[TestClass]
public class SessionControllerTests
{
    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize ParseClient with test mode and ensure it's globally available
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();

        // Register valid classes that will be used in the tests
        Client.AddValidClass<ParseUser>();
        Client.AddValidClass<ParseSession>();
    }


    [TestMethod]
    public async Task TestGetSessionWithEmptyResultAsync()
    {
        var controller = new ParseSessionController(
            CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null)).Object,
            Client.Decoder
        );

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
        {
            await controller.GetSessionAsync("S0m3Se551on", Client, CancellationToken.None);
        });
    }

    [TestMethod]
    public async Task TestGetSessionAsync()
    {
        var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.Accepted,
            new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "Session",
                ["sessionToken"] = "S0m3Se551on",
                ["restricted"] = true
            }
        );

        var mockRunner = CreateMockRunner(response);
        var controller = new ParseSessionController(mockRunner.Object, Client.Decoder);

        var session = await controller.GetSessionAsync("S0m3Se551on", Client, CancellationToken.None);
     
        // Assertions
        Assert.IsNotNull(session);
        Assert.AreEqual(4, session.Count());
        Assert.IsTrue((bool) session["restricted"]);
        Assert.AreEqual("S0m3Se551on", session["sessionToken"]);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "sessions/me"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task TestRevokeAsync()
    {
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, default)
        );

        var controller = new ParseSessionController(mockRunner.Object, Client.Decoder);
        await controller.RevokeAsync("S0m3Se551on", CancellationToken.None);

        // Assertions
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "logout"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task TestUpgradeToRevocableSessionAsync()
    {
        var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.Accepted,
            new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "Session",
                ["sessionToken"] = "S0m3Se551on",
                ["restricted"] = true
            }
        );

        var mockRunner = CreateMockRunner(response);
        var controller = new ParseSessionController(mockRunner.Object, Client.Decoder);

        var session = await controller.UpgradeToRevocableSessionAsync("S0m3Se551on", Client, CancellationToken.None);
        foreach (var item in session)
        {
            Debug.Write(item.Key);
            Debug.Write(" Val : ");
            Debug.Write(item.Value);
        }
        // Assertions
        Assert.IsNotNull(session);
        Assert.AreEqual(4, session.Count());
        Assert.IsTrue((bool) session["restricted"]);
        Assert.AreEqual("S0m3Se551on", session["sessionToken"]);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "upgradeToRevocableSession"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public void TestIsRevocableSessionToken()
    {
        var sessionController = new ParseSessionController(Mock.Of<IParseCommandRunner>(), Client.Decoder);

        Assert.IsTrue(sessionController.IsRevocableSessionToken("r:session"));
        Assert.IsTrue(sessionController.IsRevocableSessionToken("r:session:r:"));
        Assert.IsTrue(sessionController.IsRevocableSessionToken("session:r:"));
        Assert.IsFalse(sessionController.IsRevocableSessionToken("session:s:d:r"));
        Assert.IsFalse(sessionController.IsRevocableSessionToken("s:ession:s:d:r"));
        Assert.IsFalse(sessionController.IsRevocableSessionToken(""));
    }

    private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
    {
        var mockRunner = new Mock<IParseCommandRunner>();
        mockRunner
            .Setup(obj => obj.RunCommandAsync(
                It.IsAny<ParseCommand>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return mockRunner;
    }
}
