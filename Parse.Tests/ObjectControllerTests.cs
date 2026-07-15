using System;
using System.Collections.Generic;
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

namespace Parse.Tests;

[TestClass]
public class ObjectControllerTests
{

    private ParseClient Client { get; set; }
    [TestInitialize]
    public void SetUp()
    {
        // Initialize the client and ensure the instance is set
        Client = new ParseClient(new ServerConnectionData { Test = true , ApplicationID = "", Key = ""});
        Client.Publicize();
    }
    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();


    [TestMethod]
    public async Task TestFetchAsync()
    {
        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.Accepted,
            new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "Corgi",
                ["objectId"] = "st4nl3yW",
                ["doge"] = "isShibaInu",
                ["createdAt"] = "2015-09-18T18:11:28.943Z"
            }
        ));

        var controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);

        var newState = await controller.FetchAsync(
            new MutableObjectState
            {
                ClassName = "Corgi",
                ObjectId = "st4nl3yW",
                ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
            },
            default,
            Client,
            CancellationToken.None
        );

        // Assert
        Assert.IsNotNull(newState);
        Assert.AreEqual("isShibaInu", newState["doge"]);
        Assert.IsFalse(newState.ContainsKey("corgi"));
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "classes/Corgi/st4nl3yW"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestSaveAsync()
    {
        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.Accepted,
            new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "Corgi",
                ["objectId"] = "st4nl3yW",
                ["doge"] = "isShibaInu",
                ["createdAt"] = "2015-09-18T18:11:28.943Z"
            }
        ));

        var controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);

        var newState = await controller.SaveAsync(
            new MutableObjectState
            {
                ClassName = "Corgi",
                ObjectId = "st4nl3yW",
                ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
            },
            new Dictionary<string, IParseFieldOperation>
            {
                ["gogo"] = new Mock<IParseFieldOperation>().Object
            },
            default,
            Client,
            CancellationToken.None
        );

        // Assert
        Assert.IsNotNull(newState);
        Assert.AreEqual("isShibaInu", newState["doge"]);
        Assert.IsFalse(newState.ContainsKey("corgi"));
        Assert.IsFalse(newState.ContainsKey("gogo"));
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "classes/Corgi/st4nl3yW"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestSaveNewObjectAsync()
    {
        var state = new MutableObjectState
        {
            ClassName = "Corgi",
            ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
        };

        var operations = new Dictionary<string, IParseFieldOperation>
        {
            ["gogo"] = new Mock<IParseFieldOperation>().Object
        };

        var responseDict = new Dictionary<string, object>
        {
            ["__type"] = "Object",
            ["className"] = "Corgi",
            ["objectId"] = "st4nl3yW",
            ["doge"] = "isShibaInu",
            ["createdAt"] = "2015-09-18T18:11:28.943Z"
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.Created,
            responseDict
        ));

        var controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);

        var newState = await controller.SaveAsync(state, operations, default, Client, CancellationToken.None);

        // Assert
        Assert.IsNotNull(newState);
        Assert.AreEqual("st4nl3yW", newState.ObjectId);
        Assert.AreEqual("isShibaInu", newState["doge"]);
        Assert.IsFalse(newState.ContainsKey("corgi"));
        Assert.IsFalse(newState.ContainsKey("gogo"));
        Assert.IsNotNull(newState.CreatedAt);
        Assert.IsNotNull(newState.UpdatedAt);

        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "classes/Corgi"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestDeleteAsync()
    {
        var state = new MutableObjectState
        {
            ClassName = "Corgi",
            ObjectId = "st4nl3yW",
            ServerData = new Dictionary<string, object> { ["corgi"] = "isNotDoge" }
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.OK,
            new Dictionary<string, object>()
        ));

        var controller = new ParseObjectController(mockRunner.Object, Client.Decoder, Client.ServerConnectionData);

        await controller.DeleteAsync(state, default, CancellationToken.None);

        // Assert
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "classes/Corgi/st4nl3yW"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
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
