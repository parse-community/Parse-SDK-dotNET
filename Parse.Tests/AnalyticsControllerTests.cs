using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Analytics;
using Parse.Infrastructure.Execution;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Parse.Tests;

[TestClass]
public class AnalyticsControllerTests
{
    ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

    [TestMethod]
    public void TestTrackEventWithEmptyDimensions()
    {
        // Arrange: Mock the Parse command runner to return an accepted status with an empty dictionary
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>())
        );

        var analyticsController = new ParseAnalyticsController(mockRunner.Object);

        // Act: Call TrackEventAsync with empty dimensions
        var result = analyticsController.TrackEventAsync(
            "SomeEvent",
            dimensions: null,
            sessionToken: null,
            serviceHub: Client,
            cancellationToken: CancellationToken.None
        );

        // Assert: Verify the task was successful
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(Task)); // If the method has a result type, adjust accordingly.
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "events/SomeEvent"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestTrackEventWithNonEmptyDimensions()
    {
        // Arrange: Create a mock runner that simulates a response with accepted status
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>())
        );

        var analyticsController = new ParseAnalyticsController(mockRunner.Object);
        var dimensions = new Dictionary<string, string>
        {
            ["njwerjk12"] = "5523dd"
        };

        // Act: Call TrackEventAsync with non-empty dimensions
        await analyticsController.TrackEventAsync(
            "SomeEvent",
            dimensions: dimensions,
            sessionToken: null,
            serviceHub: Client,
            cancellationToken: CancellationToken.None
        );

        // Assert: Verify the command was sent with the correct path and content
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command =>
                    command.Path.Contains("events/SomeEvent") &&
                    ValidateDimensions(command.Data, dimensions)
                ),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }


    /// <summary>
    /// Validates that the dimensions dictionary is correctly serialized into the command's Data stream.
    /// </summary>
    private static bool ValidateDimensions(Stream dataStream, IDictionary<string, string> expectedDimensions)
    {
        if (dataStream == null)
        {
            return false;
        }

        // Read and deserialize the stream content
        dataStream.Position = 0; // Reset the stream position
        using var reader = new StreamReader(dataStream);
        var content = reader.ReadToEnd();

        // Parse the JSON content
        var parsedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

        // Ensure dimensions are present and correct
        if (parsedData.TryGetValue("dimensions", out var dimensionsObj) &&
            dimensionsObj is JObject dimensionsJson)
        {
            var dimensions = dimensionsJson.ToObject<Dictionary<string, string>>();
            if (dimensions == null)
            {
                return false;
            }

            foreach (var pair in expectedDimensions)
            {
                if (!dimensions.TryGetValue(pair.Key, out var value) || value != pair.Value)
                {
                    return false; // Mismatch found
                }
            }

            // Ensure no extra dimensions are present
            return dimensions.Count == expectedDimensions.Count;
        }

        return false;
    }



    [TestMethod]
    public async Task TestTrackAppOpenedWithEmptyPushHash()
    {
        // Arrange: Mock the ParseCommandRunner to simulate an accepted response
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>())
        );

        var analyticsController = new ParseAnalyticsController(mockRunner.Object);

        // Act: Call TrackAppOpenedAsync with a null push hash
        await analyticsController.TrackAppOpenedAsync(
            pushHash: null,
            sessionToken: null,
            serviceHub: Client,
            cancellationToken: CancellationToken.None
        );

        // Assert: Verify that the appropriate command was sent
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => command.Path == "events/AppOpened"),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestTrackAppOpenedWithNonEmptyPushHash()
    {
        // Arrange: Mock the ParseCommandRunner to simulate an accepted response
        var mockRunner = CreateMockRunner(
            new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>())
        );

        var analyticsController = new ParseAnalyticsController(mockRunner.Object);

        // Act: Call TrackAppOpenedAsync with a non-empty push hash
        await analyticsController.TrackAppOpenedAsync(
            pushHash: "32j4hll12lkk",
            sessionToken: null,
            serviceHub: Client,
            cancellationToken: CancellationToken.None
        );

        // Assert: Verify that the command was sent exactly once
        mockRunner.Verify(
            obj => obj.RunCommandAsync(
                It.Is<ParseCommand>(command => ValidateCommand(command, "32j4hll12lkk")),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    /// <summary>
    /// Validates the ParseCommand for the given push hash.
    /// </summary>
    private bool ValidateCommand(ParseCommand command, string expectedPushHash)
    {
        if (command.Path != "events/AppOpened")
        {
            return false;
        }

        var dataStream = command.Data;
        if (dataStream == null)
        {
            return false;
        }

        // Read and deserialize the stream
        dataStream.Position = 0;
        using var reader = new StreamReader(dataStream);
        var jsonContent = reader.ReadToEnd();
        var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

        // Validate the push_hash
        return dataDictionary != null &&
               dataDictionary.ContainsKey("push_hash") &&
               dataDictionary["push_hash"].ToString() == expectedPushHash;
    }


    Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
    {
        var mockRunner = new Mock<IParseCommandRunner>();

        // Setup the mock to return a Task with the expected Tuple
        mockRunner.Setup(obj => obj.RunCommandAsync(
            It.IsAny<ParseCommand>(),
            It.IsAny<IProgress<IDataTransferLevel>>(),
            It.IsAny<IProgress<IDataTransferLevel>>(),
            It.IsAny<CancellationToken>()
        ))
        .Returns(Task.FromResult(response));  // Return the tuple as part of the Task

        return mockRunner;
    }

}
