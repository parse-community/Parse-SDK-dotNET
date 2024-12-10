using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure.Execution;
using Parse.Platform.Files;

namespace Parse.Tests;

[TestClass]
public class FileControllerTests
{
    [TestMethod]
    public async Task TestFileControllerSaveWithInvalidResultAsync()
    {
        var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
        var mockRunner = CreateMockRunner(response);

        var state = new FileState
        {
            Name = "bekti.png",
            MediaType = "image/png"
        };

        var controller = new ParseFileController(mockRunner.Object);

        await Assert.ThrowsExceptionAsync<NullReferenceException>(async () =>
        {
            await controller.SaveAsync(state, new MemoryStream(), null, null);
        });
    }

    [TestMethod]
    public async Task TestFileControllerSaveWithEmptyResultAsync()
    {
        var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>());
        var mockRunner = CreateMockRunner(response);

        var state = new FileState
        {
            Name = "bekti.png",
            MediaType = "image/png"
        };

        var controller = new ParseFileController(mockRunner.Object);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
        {
            await controller.SaveAsync(state, new MemoryStream(), null, null);
        });
    }

    [TestMethod]
    public async Task TestFileControllerSaveWithIncompleteResultAsync()
    {
        var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { ["name"] = "newBekti.png" });
        var mockRunner = CreateMockRunner(response);

        var state = new FileState
        {
            Name = "bekti.png",
            MediaType = "image/png"
        };

        var controller = new ParseFileController(mockRunner.Object);

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
        {
            await controller.SaveAsync(state, new MemoryStream(), null, null);
        });
    }

    [TestMethod]
    public async Task TestFileControllerSaveAsync()
    {
        var state = new FileState
        {
            Name = "bekti.png",
            MediaType = "image/png"
        };

        var mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(
            HttpStatusCode.Accepted,
            new Dictionary<string, object>
            {
                ["name"] = "newBekti.png",
                ["url"] = "https://www.parse.com/newBekti.png"
            }));

        var controller = new ParseFileController(mockRunner.Object);
        var newState = await controller.SaveAsync(state, new MemoryStream(), null, null);

        // Assertions
        Assert.AreEqual(state.MediaType, newState.MediaType);
        Assert.AreEqual("newBekti.png", newState.Name);
        Assert.AreEqual("https://www.parse.com/newBekti.png", newState.Location.AbsoluteUri);
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
