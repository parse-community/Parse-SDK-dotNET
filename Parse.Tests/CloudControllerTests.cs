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

#warning Class refactoring requires completion.

[TestClass]
public class CloudControllerTests
{
    ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

    [TestMethod]
    public async Task TestEmptyCallFunction()
    {
        // Arrange: Create a mock runner that simulates a response with an accepted status but no data
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null)
        );

        var controller = new ParseCloudCodeController(mockRunner.Object, Client.Decoder);

        // Act & Assert: Call the function and verify the task faults as expected
        try
        {
            await controller.CallFunctionAsync<string>("someFunction", null, null, Client, CancellationToken.None);
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
        // Arrange: Create a mock runner with a predefined response
        var responseDict = new Dictionary<string, object> { ["result"] = "gogo" };
        var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
        var mockRunner = CreateMockRunner(response);

        var cloudCodeController = new ParseCloudCodeController(mockRunner.Object, Client.Decoder);

        // Act: Call the function and capture the result
        var result = await cloudCodeController.CallFunctionAsync<string>(
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
        // Arrange: Create a mock runner with a complex type response
        var complexResponse = new Dictionary<string, object>
    {
        { "result", new Dictionary<string, object>
            {
                { "fosco", "ben" },
                { "list", new List<object> { 1, 2, 3 } }
            }
        }
    };
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, complexResponse)
        );

        var cloudCodeController = new ParseCloudCodeController(mockRunner.Object, Client.Decoder);

        // Act: Call the function with a complex return type
        var result = await cloudCodeController.CallFunctionAsync<IDictionary<string, object>>(
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
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, wrongTypeResponse)
        );

        var cloudCodeController = new ParseCloudCodeController(mockRunner.Object, Client.Decoder);

        // Act & Assert: Expect the call to fail with a ParseFailureException || This is fun!

        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
        {
            await cloudCodeController.CallFunctionAsync<int>(
                "someFunction",
                parameters: null,
                sessionToken: null,
                serviceHub: Client,
                cancellationToken: CancellationToken.None
            );
        });
    }



    private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
    {
        var mockRunner = new Mock<IParseCommandRunner>();
        mockRunner.Setup(obj => obj.RunCommandAsync(
            It.IsAny<ParseCommand>(),
            It.IsAny<IProgress<IDataTransferLevel>>(),
            It.IsAny<IProgress<IDataTransferLevel>>(),
            It.IsAny<CancellationToken>()
        )).Returns(Task.FromResult(response));

        return mockRunner;
    }

}
