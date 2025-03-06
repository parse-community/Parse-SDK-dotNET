using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
    private Mock<IParseCommandRunner> _commandRunnerMock;
    private Mock<IParseDataDecoder> _decoderMock;
    private MutableServiceHub _hub;
    private ParseClient _client;

    [TestInitialize]
    public void Initialize()
    {
        _commandRunnerMock = new Mock<IParseCommandRunner>();
        _decoderMock = new Mock<IParseDataDecoder>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _commandRunnerMock = null;
        _decoderMock = null;
        _hub = null;
        _client = null;

    }



    private void SetupMocksForMissingResult()
    {
        _commandRunnerMock
            .Setup(runner => runner.RunCommandAsync(
                It.IsAny<ParseCommand>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new Tuple<HttpStatusCode, IDictionary<string, object>>(
                HttpStatusCode.OK,
                new Dictionary<string, object>
                {
                    ["unexpectedKey"] = "unexpectedValue" // Missing "result" key
                }));

        _decoderMock
            .Setup(decoder => decoder.Decode(It.IsAny<object>(), It.IsAny<IServiceHub>()))
            .Returns(new Dictionary<string, object> { ["unexpectedKey"] = "unexpectedValue" });
    }



    [TestMethod]
    public async Task TestCloudFunctionsMissingResultAsync()
    {
        // Arrange
        SetupMocksForMissingResult();

        _hub = new MutableServiceHub
        {
            CommandRunner = _commandRunnerMock.Object,
            CloudCodeController = new ParseCloudCodeController(_commandRunnerMock.Object, _decoderMock.Object)
        };

        _client = new ParseClient(new ServerConnectionData { Test = true }, _hub);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
            await _client.CallCloudCodeFunctionAsync<IDictionary<string, object>>("someFunction", null, CancellationToken.None));
    }

    [TestMethod]
    public async Task TestParseCloudCodeControllerMissingResult()
    {
        //Arrange
        SetupMocksForMissingResult();
        var controller = new ParseCloudCodeController(_commandRunnerMock.Object, _decoderMock.Object);

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