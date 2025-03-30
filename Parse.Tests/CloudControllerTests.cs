
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Cloud;

namespace Parse.Tests;

[TestClass]
public class CloudControllerTests
{
    private Mock<IParseCommandRunner> _mockRunner;
    private ParseCloudCodeController _cloudCodeController;
    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });
        _mockRunner = new Mock<IParseCommandRunner>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mockRunner = null;
        _cloudCodeController = null;
        Client = null;
    }


    [TestMethod]
    public async Task TestEmptyCallFunction()
    {
        // Arrange: Setup mock runner and controller

        _mockRunner.Setup(obj => obj.RunCommandAsync(
           It.IsAny<ParseCommand>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<CancellationToken>()
       )).Returns(Task.FromResult(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null)));

        _cloudCodeController = new ParseCloudCodeController(_mockRunner.Object, Client.Decoder);


        // Act & Assert: Call the function and verify the task faults as expected
        try
        {
            await _cloudCodeController.CallFunctionAsync<string>("someFunction", null, null, Client, CancellationToken.None);
            Assert.Fail("Expected the task to fault, but it succeeded.");
        }
        catch (ParseFailureException ex)
        {
            Assert.AreEqual(ParseFailureException.ErrorCode.OtherCause, ex.Code);
            Assert.AreEqual("Cloud function returned no data.", ex.Message);
        }
    }


    [TestMethod]
    public async Task TestCallFunction()
    {
        // Arrange: Setup mock runner and controller with a response
        var responseDict = new Dictionary<string, object> { ["result"] = "gogo" };
        _mockRunner.Setup(obj => obj.RunCommandAsync(
           It.IsAny<ParseCommand>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<CancellationToken>()
       )).Returns(Task.FromResult(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict)));

        _cloudCodeController = new ParseCloudCodeController(_mockRunner.Object, Client.Decoder);


        // Act: Call the function and capture the result
        var result = await _cloudCodeController.CallFunctionAsync<string>(
            "someFunction",
            parameters: null,
            sessionToken: null,
            serviceHub: Client,
            cancellationToken: CancellationToken.None
        );

        // Assert: Verify the result is as expected
        Assert.IsNotNull(result);
        Assert.AreEqual("gogo", result); // Ensure the result matches the mock response
    }


    [TestMethod]
    public async Task TestCallFunctionWithComplexType()
    {
        // Arrange: Setup mock runner and controller with a complex type response
        var complexResponse = new Dictionary<string, object>
    {
        { "result", new Dictionary<string, object>
         {
             { "fosco", "ben" },
             { "list", new List<object> { 1, 2, 3 } }
         }
        }
    };

        _mockRunner.Setup(obj => obj.RunCommandAsync(
           It.IsAny<ParseCommand>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<CancellationToken>()
       )).Returns(Task.FromResult(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, complexResponse)));

        _cloudCodeController = new ParseCloudCodeController(_mockRunner.Object, Client.Decoder);


        // Act: Call the function with a complex return type
        var result = await _cloudCodeController.CallFunctionAsync<IDictionary<string, object>>(
            "someFunction",
            parameters: null,
            sessionToken: null,
            serviceHub: Client,
            cancellationToken: CancellationToken.None
        );

        // Assert: Validate the returned complex type
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IDictionary<string, object>));
        Assert.AreEqual("ben", result["fosco"]);
        Assert.IsInstanceOfType(result["list"], typeof(IList<object>));
    }

    [TestMethod]
    public async Task TestCallFunctionWithWrongType()
    {
        // a mock runner with a response that doesn't match the expected type

        var wrongTypeResponse = new Dictionary<string, object>
 {
     { "result", "gogo" }
 };

        _mockRunner.Setup(obj => obj.RunCommandAsync(
           It.IsAny<ParseCommand>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<IProgress<IDataTransferLevel>>(),
           It.IsAny<CancellationToken>()
       )).Returns(Task.FromResult(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, wrongTypeResponse)));


        _cloudCodeController = new ParseCloudCodeController(_mockRunner.Object, Client.Decoder);

        // Act & Assert: Expect the call to fail with a ParseFailureException || This is fun!

        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
        {
            await _cloudCodeController.CallFunctionAsync<int>(
                "someFunction",
                parameters: null,
                sessionToken: null,
                serviceHub: Client,
                cancellationToken: CancellationToken.None
            );
        });
    }
}