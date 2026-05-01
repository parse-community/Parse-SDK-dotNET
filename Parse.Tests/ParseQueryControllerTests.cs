using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Parse.Abstractions.Internal;
using Moq;

using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;
using Parse.Platform.Queries;

namespace Parse.Tests;

[TestClass]
public class ParseQueryControllerTests
{
    private Mock<IParseCommandRunner> mockCommandRunner;
    private IServiceHub serviceHub;

    [TestInitialize]
    public void SetUp()
    {
        mockCommandRunner = new Mock<IParseCommandRunner>();

        var hub = new MutableServiceHub
        {
            CommandRunner = mockCommandRunner.Object
        };
        hub.SetDefaults(); // This correctly initializes all dependencies, including ClassController.

        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);
        serviceHub = client.Services;
    }

    [TestMethod]
    [Description("Tests that FindAsync correctly decodes a list of objects.")]
    public async Task FindAsync_WithResults_ReturnsDecodedStates()
    {
        // Arrange
        var controller = new ParseQueryController(mockCommandRunner.Object, serviceHub.Decoder);
        var query = new ParseQuery<ParseObject>(serviceHub, "TestClass");
        var serverResponse = new Dictionary<string, object>
        {
            ["results"] = new List<object>
                {
                    new Dictionary<string, object> { ["__type"] = "Object", ["className"] = "TestClass", ["objectId"] = "obj1" },
                    new Dictionary<string, object> { ["__type"] = "Object", ["className"] = "TestClass", ["objectId"] = "obj2" }
                }
        };
        var tupleResponse = new Tuple<System.Net.HttpStatusCode, IDictionary<string, object>>(System.Net.HttpStatusCode.OK, serverResponse);

        mockCommandRunner.Setup(r => r.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tupleResponse);

        // Act
        var result = await controller.FindAsync(query, null, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.AreEqual("obj1", result.First().ObjectId);
    }

    [TestMethod]
    [Description("Tests that CountAsync returns the correct count from the server response.")]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var controller = new ParseQueryController(mockCommandRunner.Object, serviceHub.Decoder);
        var query = new ParseQuery<ParseObject>(serviceHub, "TestClass");
        var serverResponse = new Dictionary<string, object> { ["count"] = 150 };
        var tupleResponse = new Tuple<System.Net.HttpStatusCode, IDictionary<string, object>>(System.Net.HttpStatusCode.OK, serverResponse);

        mockCommandRunner.Setup(r => r.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tupleResponse);

        // Act
        int count = await controller.CountAsync(query, null, CancellationToken.None);

        // Assert
        Assert.AreEqual(150, count);
    }

    [TestMethod]
    [Description("Tests that FirstAsync returns the correctly decoded first object.")]
    public async Task FirstAsync_ReturnsFirstObject()
    {
        // Arrange
        var controller = new ParseQueryController(mockCommandRunner.Object, serviceHub.Decoder);
        var query = new ParseQuery<ParseObject>(serviceHub, "TestClass");
        var serverResponse = new Dictionary<string, object>
        {
            ["results"] = new List<object> { new Dictionary<string, object> { ["__type"] = "Object", ["className"] = "TestClass", ["objectId"] = "theFirst" } }
        };
        var tupleResponse = new Tuple<System.Net.HttpStatusCode, IDictionary<string, object>>(System.Net.HttpStatusCode.OK, serverResponse);

        mockCommandRunner.Setup(r => r.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tupleResponse);

        // Act
        var result = await controller.FirstAsync(query, null, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("theFirst", result.ObjectId);
    }
}