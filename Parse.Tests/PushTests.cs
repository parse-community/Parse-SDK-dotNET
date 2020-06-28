using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Push;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure;
using Parse.Platform.Push;

namespace Parse.Tests
{
    [TestClass]
    public class PushTests
    {
        ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

        IParsePushController GetMockedPushController(IPushState expectedPushState)
        {
            Mock<IParsePushController> mockedController = new Mock<IParsePushController>(MockBehavior.Strict);
            mockedController.Setup(obj => obj.SendPushNotificationAsync(It.Is<IPushState>(s => s.Equals(expectedPushState)), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            return mockedController.Object;
        }

        IParsePushChannelsController GetMockedPushChannelsController(IEnumerable<string> channels)
        {
            Mock<IParsePushChannelsController> mockedChannelsController = new Mock<IParsePushChannelsController>(MockBehavior.Strict);
            mockedChannelsController.Setup(obj => obj.SubscribeAsync(It.Is<IEnumerable<string>>(it => it.CollectionsEqual(channels)), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            mockedChannelsController.Setup(obj => obj.UnsubscribeAsync(It.Is<IEnumerable<string>>(it => it.CollectionsEqual(channels)), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            return mockedChannelsController.Object;
        }

        [TestCleanup]
        public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        [AsyncStateMachine(typeof(PushTests))]
        public Task TestSendPush()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            MutablePushState state = new MutablePushState
            {
                Query = Client.GetInstallationQuery()
            };

            ParsePush thePush = new ParsePush(client);

            hub.PushController = GetMockedPushController(state);

            thePush.Alert = "Alert";
            state.Alert = "Alert";

            return thePush.SendAsync().ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);

                thePush.Channels = new List<string> { { "channel" } };
                state.Channels = new List<string> { { "channel" } };

                return thePush.SendAsync();
            }).Unwrap().ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);

                ParseQuery<ParseInstallation> query = new ParseQuery<ParseInstallation>(client, "aClass");

                thePush.Query = query;
                state.Query = query;

                return thePush.SendAsync();
            }).Unwrap().ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(PushTests))]
        public Task TestSubscribe()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            List<string> channels = new List<string> { };

            hub.PushChannelsController = GetMockedPushChannelsController(channels);

            channels.Add("test");

            return client.SubscribeToPushChannelAsync("test").ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);

                return client.SubscribeToPushChannelsAsync(new List<string> { "test" });
            }).Unwrap().ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource { };
                return client.SubscribeToPushChannelsAsync(new List<string> { "test" }, cancellationTokenSource.Token);
            }).Unwrap().ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(PushTests))]
        public Task TestUnsubscribe()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            List<string> channels = new List<string> { };

            hub.PushChannelsController = GetMockedPushChannelsController(channels);

            channels.Add("test");

            return client.UnsubscribeToPushChannelAsync("test").ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);

                return client.UnsubscribeToPushChannelsAsync(new List<string> { { "test" } });
            }).ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource { };
                return client.UnsubscribeToPushChannelsAsync(new List<string> { { "test" } }, cancellationTokenSource.Token);
            }).ContinueWith(task =>
            {
                Assert.IsTrue(task.IsCompleted);
                Assert.IsFalse(task.IsFaulted);
            });
        }
    }
}
