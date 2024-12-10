using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure.Execution;
using WebRequest = Parse.Infrastructure.Execution.WebRequest;

namespace Parse.Tests;

[TestClass]
public class CommandTests
{
    private ParseClient Client { get; set; }

    [TestInitialize]
    public void Initialize() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

    [TestCleanup]
    public void Clean() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestMakeCommand()
    {
        var command = new ParseCommand("endpoint", method: "GET", sessionToken: "abcd", headers: default, data: default);

        Assert.AreEqual("endpoint", command.Path);
        Assert.AreEqual("GET", command.Method);
        Assert.IsTrue(command.Headers.Any(pair => pair.Key == "X-Parse-Session-Token" && pair.Value == "abcd"));
    }

    [TestMethod]
    public async Task TestRunCommandAsync()
    {
        // Arrange
        var mockHttpClient = new Mock<IWebClient>();
        var mockInstallationController = new Mock<IParseInstallationController>();

        mockHttpClient
            .Setup(obj => obj.ExecuteAsync(
                It.IsAny<WebRequest>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "{}"));

        mockInstallationController
            .Setup(installation => installation.GetAsync())
            .ReturnsAsync(default(Guid?));

        var commandRunner = new ParseCommandRunner(
            mockHttpClient.Object,
            mockInstallationController.Object,
            Client.MetadataController,
            Client.ServerConnectionData,
            new Lazy<IParseUserController>(() => Client.UserController)
        );

        // Act
        var result = await commandRunner.RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.Item1);
        Assert.IsInstanceOfType(result.Item2, typeof(IDictionary<string, object>));
        Assert.AreEqual(0, result.Item2.Count);
    }

    [TestMethod]
    public async Task TestRunCommandWithArrayResultAsync()
    {
        // Arrange
        var mockHttpClient = new Mock<IWebClient>();
        var mockInstallationController = new Mock<IParseInstallationController>();

        mockHttpClient
            .Setup(obj => obj.ExecuteAsync(It.IsAny<WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "[]"));

        mockInstallationController
            .Setup(installation => installation.GetAsync())
            .ReturnsAsync(default(Guid?));

        var commandRunner = new ParseCommandRunner(
            mockHttpClient.Object,
            mockInstallationController.Object,
            Client.MetadataController,
            Client.ServerConnectionData,
            new Lazy<IParseUserController>(() => Client.UserController)
        );

        // Act
        var result = await commandRunner.RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.Item1);
        Assert.IsTrue(result.Item2.ContainsKey("results"));
        Assert.IsInstanceOfType(result.Item2["results"], typeof(IList<object>));
    }

    [TestMethod]
    public async Task TestRunCommandWithInvalidStringAsync()
    {
        // Arrange: Mock an invalid response
        var mockHttpClient = new Mock<IWebClient>();
        var mockInstallationController = new Mock<IParseInstallationController>();

        mockHttpClient
            .Setup(obj => obj.ExecuteAsync(It.IsAny<WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "invalid")); // Mock an invalid response

        mockInstallationController
            .Setup(controller => controller.GetAsync())
            .ReturnsAsync(default(Guid?));

        var commandRunner = new ParseCommandRunner(
            mockHttpClient.Object,
            mockInstallationController.Object,
            Client.MetadataController,
            Client.ServerConnectionData,
            new Lazy<IParseUserController>(() => Client.UserController)
        );

        // Act: Run the command
        var result = await commandRunner.RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null));

        // Assert: Check for BadRequest and appropriate error message
        Assert.AreEqual(HttpStatusCode.BadRequest, result.Item1); // Response status should indicate BadRequest
        Assert.IsNotNull(result.Item2); // Content should not be null
        Assert.IsTrue(result.Item2.ContainsKey("error")); // Ensure the error key is present
        Assert.AreEqual("Invalid or alternatively-formatted response received from server.", result.Item2["error"]); // Verify error message
    }

    [TestMethod]
    public async Task TestRunCommandWithErrorCodeAsync()
    {
        // Arrange
        var mockHttpClient = new Mock<IWebClient>();
        var mockInstallationController = new Mock<IParseInstallationController>();

        mockHttpClient
            .Setup(obj => obj.ExecuteAsync(It.IsAny<WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, "{ \"code\": 101, \"error\": \"Object not found.\" }"));

        mockInstallationController
            .Setup(controller => controller.GetAsync())
            .ReturnsAsync(default(Guid?));

        var commandRunner = new ParseCommandRunner(
            mockHttpClient.Object,
            mockInstallationController.Object,
            Client.MetadataController,
            Client.ServerConnectionData,
            new Lazy<IParseUserController>(() => Client.UserController)
        );

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
        {
            await commandRunner.RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null));
        });
    }

    [TestMethod]
    public async Task TestRunCommandWithInternalServerErrorAsync()
    {
        // Arrange
        var mockHttpClient = new Mock<IWebClient>();
        var mockInstallationController = new Mock<IParseInstallationController>();

        mockHttpClient
            .Setup(client => client.ExecuteAsync(It.IsAny<Infrastructure.Execution.WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, null));

        mockInstallationController
            .Setup(controller => controller.GetAsync())
            .ReturnsAsync(default(Guid?));

        var commandRunner = new ParseCommandRunner(
            mockHttpClient.Object,
            mockInstallationController.Object,
            Client.MetadataController,
            Client.ServerConnectionData,
            new Lazy<IParseUserController>(() => Client.UserController)
        );

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
        {
            await commandRunner.RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null));
        });
    }
}
