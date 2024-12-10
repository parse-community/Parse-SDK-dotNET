using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Push;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure;
using Parse.Platform.Push;

namespace Parse.Tests;

[TestClass]
public class PushTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    private IParsePushController GetMockedPushController(IPushState expectedPushState)
    {
        var mockedController = new Mock<IParsePushController>(MockBehavior.Strict);
        mockedController
            .Setup(obj => obj.SendPushNotificationAsync(It.Is<IPushState>(s => s.Equals(expectedPushState)), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));

        return mockedController.Object;
    }

    private IParsePushChannelsController GetMockedPushChannelsController(IEnumerable<string> channels)
    {
        var mockedChannelsController = new Mock<IParsePushChannelsController>(MockBehavior.Strict);

        // Setup for SubscribeAsync to accept any IServiceHub instance
        mockedChannelsController
            .Setup(obj => obj.SubscribeAsync(It.Is<IEnumerable<string>>(it => it.CollectionsEqual(channels)), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Ensure it returns a completed task

        // Setup for UnsubscribeAsync to accept any IServiceHub instance
        mockedChannelsController
            .Setup(obj => obj.UnsubscribeAsync(It.Is<IEnumerable<string>>(it => it.CollectionsEqual(channels)), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Ensure it returns a completed task

        return mockedChannelsController.Object;
    }



    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public async Task TestSendPushAsync()
    {
        // Arrange
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var state = new MutablePushState
        {
            Query = Client.GetInstallationQuery()
        };

        var thePush = new ParsePush(client);

        hub.PushController = GetMockedPushController(state);

        // Act
        thePush.Alert = "Alert";
        state.Alert = "Alert";

        await thePush.SendAsync();

        thePush.Channels = new List<string> { "channel" };
        state.Channels = new List<string> { "channel" };

        await thePush.SendAsync();

        var query = new ParseQuery<ParseInstallation>(client, "aClass");
        thePush.Query = query;
        state.Query = query;

        await thePush.SendAsync();

        // Assert
        Assert.IsTrue(true); // Reaching here means no exceptions occurred
    }

    [TestMethod]
    public async Task TestSubscribeAsync()
    {
        // Arrange
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var channels = new List<string> { "test" };
        hub.PushChannelsController = GetMockedPushChannelsController(channels);

        // Act
        await client.SubscribeToPushChannelAsync("test");
        await client.SubscribeToPushChannelsAsync(new List<string> { "test" });

        using var cancellationTokenSource = new CancellationTokenSource();
        await client.SubscribeToPushChannelsAsync(new List<string> { "test" }, cancellationTokenSource.Token);

        // Assert
        Assert.IsTrue(true); // Reaching here means no exceptions occurred
    }

    [TestMethod]
    public async Task TestUnsubscribeAsync()
    {
        // Arrange
        var hub = new MutableServiceHub();
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var channels = new List<string> { "test" }; // Corrected to ensure we have the "test" channel
        hub.PushChannelsController = GetMockedPushChannelsController(channels);

        // Act
        await client.UnsubscribeToPushChannelAsync("test");
        await client.UnsubscribeToPushChannelsAsync(new List<string> { "test" });

        using var cancellationTokenSource = new CancellationTokenSource();
        await client.UnsubscribeToPushChannelsAsync(new List<string> { "test" }, cancellationTokenSource.Token);

        // Assert
        Assert.IsTrue(true); // Reaching here means no exceptions occurred
    }


}
