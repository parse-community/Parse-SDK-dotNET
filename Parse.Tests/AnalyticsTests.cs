using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;
using Parse.Abstractions.Platform.Analytics;
using Parse.Abstractions.Platform.Users;

namespace Parse.Tests;

[TestClass]
public class AnalyticsTests
{

    private Mock<IParseAnalyticsController> _mockAnalyticsController;
    private Mock<IParseCurrentUserController> _mockCurrentUserController;
    private MutableServiceHub _hub;
    private ParseClient _client;


    [TestInitialize]
    public void Initialize()
    {
        _mockAnalyticsController = new Mock<IParseAnalyticsController>();
        _mockCurrentUserController = new Mock<IParseCurrentUserController>();

        _mockCurrentUserController
            .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sessionToken");


        _hub = new MutableServiceHub
        {
            AnalyticsController = _mockAnalyticsController.Object,
            CurrentUserController = _mockCurrentUserController.Object
        };
        _client = new ParseClient(new ServerConnectionData { Test = true }, _hub);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mockAnalyticsController = null;
        _mockCurrentUserController = null;
        _hub = null;
        _client = null;
    }


    [TestMethod]
    public async Task TestTrackEvent()
    {

        // Arrange
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseAnalyticsController>();
        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        mockCurrentUserController
            .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sessionToken");

        hub.AnalyticsController = mockController.Object;
        hub.CurrentUserController = mockCurrentUserController.Object;

        // Act
        await client.TrackAnalyticsEventAsync("SomeEvent");

        // Assert
        mockController.Verify(
            obj => obj.TrackEventAsync(
                It.Is<string>(eventName => eventName == "SomeEvent"),
                It.Is<IDictionary<string, string>>(dict => dict == null),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestTrackEventWithDimension()
    {
        // Arrange
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseAnalyticsController>();
        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        mockCurrentUserController
            .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sessionToken");

        hub.AnalyticsController = mockController.Object;
        hub.CurrentUserController = mockCurrentUserController.Object;

        var dimensions = new Dictionary<string, string> { ["facebook"] = "hq" };

        // Act
        await client.TrackAnalyticsEventAsync("SomeEvent", dimensions);

        // Assert
        mockController.Verify(
            obj => obj.TrackEventAsync(
                It.Is<string>(eventName => eventName == "SomeEvent"),
                It.Is<IDictionary<string, string>>(dict => dict != null && dict.Count == 1 && dict["facebook"] == "hq"),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }

    [TestMethod]
    public async Task TestTrackAppOpened()
    {
        // Arrange
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseAnalyticsController>();
        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        mockCurrentUserController
            .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sessionToken");

        hub.AnalyticsController = mockController.Object;
        hub.CurrentUserController = mockCurrentUserController.Object;

        // Act
        await client.TrackLaunchAsync();

        // Assert
        mockController.Verify(
            obj => obj.TrackAppOpenedAsync(
                It.Is<string>(pushHash => pushHash == null),
                It.IsAny<string>(),
                It.IsAny<IServiceHub>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Exactly(1)
        );
    }
}
