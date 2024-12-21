using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Cloud;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Cloud;

namespace Parse.Tests;

[TestClass]
public class CloudTests
{
#warning Skipped post-test-evaluation cleaning method may be needed.

    // [TestCleanup]
    // public void TearDown() => ParseCorePlugins.Instance.Reset();
    [TestMethod]
    public async Task TestCloudFunctionsMissingResultAsync()
    {
        // Arrange
        var commandRunnerMock = new Mock<IParseCommandRunner>();
        var decoderMock = new Mock<IParseDataDecoder>();

        // Mock CommandRunner
        commandRunnerMock
            .Setup(runner => runner.RunCommandAsync(
                It.IsAny<ParseCommand>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new Tuple<System.Net.HttpStatusCode, IDictionary<string, object>>(
                System.Net.HttpStatusCode.OK,
                new Dictionary<string, object>
                {
                    ["unexpectedKey"] = "unexpectedValue" // Missing "result" key
                }));

        // Mock Decoder
        decoderMock
            .Setup(decoder => decoder.Decode(It.IsAny<object>(), It.IsAny<IServiceHub>()))
            .Returns(new Dictionary<string, object> { ["unexpectedKey"] = "unexpectedValue" });

        // Set up service hub
        var hub = new MutableServiceHub
        {
            CommandRunner = commandRunnerMock.Object,
            CloudCodeController = new ParseCloudCodeController(commandRunnerMock.Object, decoderMock.Object)
        };

        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
            await client.CallCloudCodeFunctionAsync<IDictionary<string, object>>("someFunction", null, CancellationToken.None));
    }

    [TestMethod]
    public async Task TestParseCloudCodeControllerMissingResult()
    {
        // Arrange
        var commandRunnerMock = new Mock<IParseCommandRunner>();
        var decoderMock = new Mock<IParseDataDecoder>();

        // Mock the CommandRunner response
        commandRunnerMock
            .Setup(runner => runner.RunCommandAsync(
                It.IsAny<ParseCommand>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new Tuple<System.Net.HttpStatusCode, IDictionary<string, object>>(
                System.Net.HttpStatusCode.OK, // Simulated HTTP status code
                new Dictionary<string, object>
                {
                    ["unexpectedKey"] = "unexpectedValue" // Missing "result" key
                }));

        // Mock the Decoder response
        decoderMock
            .Setup(decoder => decoder.Decode(It.IsAny<object>(), It.IsAny<IServiceHub>()))
            .Returns(new Dictionary<string, object> { ["unexpectedKey"] = "unexpectedValue" });

        // Initialize the controller
        var controller = new ParseCloudCodeController(commandRunnerMock.Object, decoderMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
            await controller.CallFunctionAsync<IDictionary<string, object>>(
                "testFunction",
                null,
                null,
                null,
                CancellationToken.None));
    }



}
