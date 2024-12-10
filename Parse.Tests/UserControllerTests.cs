using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;
using Parse.Platform.Users;

namespace Parse.Tests;

[TestClass]
public class UserControllerTests
{
    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

    [TestMethod]
    public async Task TestSignUpAsync()
    {
        var state = new MutableObjectState
        {
            ClassName = "_User",
            ServerData = new Dictionary<string, object>
            {
                ["username"] = "hallucinogen",
                ["password"] = "secret"
            }
        };

        var operations = new Dictionary<string, IParseFieldOperation>
        {
            ["gogo"] = new Mock<IParseFieldOperation>().Object
        };

        var responseDict = new Dictionary<string, object>
        {
            ["__type"] = "Object",
            ["className"] = "_User",
            ["objectId"] = "d3ImSh3ki",
            ["sessionToken"] = "s3ss10nt0k3n",
            ["createdAt"] = "2015-09-18T18:11:28.943Z"
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

        var controller = new ParseUserController(mockRunner.Object, Client.Decoder);
        var newState = await controller.SignUpAsync(state, operations, Client, CancellationToken.None);

        // Assertions
        Assert.IsNotNull(newState);
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "classes/_User"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task TestLogInWithUsernamePasswordAsync()
    {
        // Mock the server response for login
        var responseDict = new Dictionary<string, object>
        {
            ["__type"] = "Object",
            ["className"] = "_User",
            ["objectId"] = "d3ImSh3ki",
            ["sessionToken"] = "s3ss10nt0k3n",
            ["createdAt"] = "2015-09-18T18:11:28.943Z"
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

        var controller = new ParseUserController(mockRunner.Object, Client.Decoder);

        // Call LogInAsync
        var newState = await controller.LogInAsync("grantland", "123grantland123", Client, CancellationToken.None);

        // Assertions to check that the response was correctly processed
        Assert.IsNotNull(newState);
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);
        mockRunner.Verify(
            obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

    

    }



    [TestMethod]
    public async Task TestLogInWithAuthDataAsync()
    {
        var responseDict = new Dictionary<string, object>
        {
            ["__type"] = "Object",
            ["className"] = "_User",
            ["objectId"] = "d3ImSh3ki",
            ["sessionToken"] = "s3ss10nt0k3n",
            ["createdAt"] = "2015-09-18T18:11:28.943Z"
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

        Parse.Platform.Users.ParseUserController controller = new Parse.Platform.Users.ParseUserController(mockRunner.Object, Client.Decoder);

        // Handle null data gracefully by passing an empty dictionary if null is provided
        var authData = new Dictionary<string, object>();  // Handle null by passing an empty dictionary
        var newState = await controller.LogInAsync(authType: "facebook", data: authData, serviceHub: Client, cancellationToken: CancellationToken.None);

        // Assertions
        Assert.IsNotNull(newState);
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "users"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }



    [TestMethod]
    public async Task TestGetUserFromSessionTokenAsync()
    {
        var responseDict = new Dictionary<string, object>
        {
            ["__type"] = "Object",
            ["className"] = "_User",
            ["objectId"] = "d3ImSh3ki",
            ["sessionToken"] = "s3ss10nt0k3n",
            ["createdAt"] = "2015-09-18T18:11:28.943Z"
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

        var controller = new ParseUserController(mockRunner.Object, Client.Decoder);
        var newState = await controller.GetUserAsync("s3ss10nt0k3n", Client, CancellationToken.None);

        // Assertions
        Assert.IsNotNull(newState);
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "users/me"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task TestRequestPasswordResetAsync()
    {
        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>()));

        var controller = new ParseUserController(mockRunner.Object, Client.Decoder);
        await controller.RequestPasswordResetAsync("gogo@parse.com", CancellationToken.None);

        // Assertions
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "requestPasswordReset"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
    {
        var mockRunner = new Mock<IParseCommandRunner>();
        mockRunner
            .Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return mockRunner;
    }
}
