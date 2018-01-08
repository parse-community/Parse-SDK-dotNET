using Moq;
using Parse;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Push.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParseTest
{
    [TestClass]
    public class PushTests
    {
        private IParsePushController GetMockedPushController(IPushState expectedPushState)
        {
            Mock<IParsePushController> mockedController = new Mock<IParsePushController>(MockBehavior.Strict);

            mockedController.Setup(
              obj => obj.SendPushNotificationAsync(
                It.Is<IPushState>(s => s.Equals(expectedPushState)),
                It.IsAny<CancellationToken>()
              )
            ).Returns(Task.FromResult(false));

            return mockedController.Object;
        }

        private IParsePushChannelsController GetMockedPushChannelsController(IEnumerable<string> channels)
        {
            Mock<IParsePushChannelsController> mockedChannelsController = new Mock<IParsePushChannelsController>(MockBehavior.Strict);

            mockedChannelsController.Setup(
              obj => obj.SubscribeAsync(
                It.Is<IEnumerable<string>>(it => it.CollectionsEqual(channels)),
                It.IsAny<CancellationToken>()
              )
            ).Returns(Task.FromResult(false));

            mockedChannelsController.Setup(
              obj => obj.UnsubscribeAsync(
                It.Is<IEnumerable<string>>(it => it.CollectionsEqual(channels)),
                It.IsAny<CancellationToken>()
              )
            ).Returns(Task.FromResult(false));

            return mockedChannelsController.Object;
        }

        [TestCleanup]
        public void TearDown()
        {
            ParseCorePlugins.Instance = null;
            ParsePushPlugins.Instance = null;
        }

        [TestMethod]
        [AsyncStateMachine(typeof(PushTests))]
        public Task TestSendPush()
        {
            MutablePushState state = new MutablePushState
            {
                Query = ParseInstallation.Query
            };

            ParsePush thePush = new ParsePush();
            ParsePushPlugins.Instance = new ParsePushPlugins
            {
                PushController = GetMockedPushController(state)
            };

            thePush.Alert = "Alert";
            state.Alert = "Alert";

            return thePush.SendAsync().ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);

                thePush.Channels = new List<string> { { "channel" } };
                state.Channels = new List<string> { { "channel" } };

                return thePush.SendAsync();
            }).Unwrap().ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);

                ParseQuery<ParseInstallation> query = new ParseQuery<ParseInstallation>("aClass");
                thePush.Query = query;
                state.Query = query;

                return thePush.SendAsync();
            }).Unwrap().ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(PushTests))]
        public Task TestSubscribe()
        {
            List<string> channels = new List<string>();
            ParsePushPlugins.Instance = new ParsePushPlugins
            {
                PushChannelsController = GetMockedPushChannelsController(channels)
            };

            channels.Add("test");
            return ParsePush.SubscribeAsync("test").ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);

                return ParsePush.SubscribeAsync(new List<string> { { "test" } });
            }).Unwrap().ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);

                CancellationTokenSource cts = new CancellationTokenSource();
                return ParsePush.SubscribeAsync(new List<string> { { "test" } }, cts.Token);
            }).Unwrap().ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(PushTests))]
        public Task TestUnsubscribe()
        {
            List<string> channels = new List<string>();
            ParsePushPlugins.Instance = new ParsePushPlugins
            {
                PushChannelsController = GetMockedPushChannelsController(channels)
            };

            channels.Add("test");
            return ParsePush.UnsubscribeAsync("test").ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);

                return ParsePush.UnsubscribeAsync(new List<string> { { "test" } });
            }).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);

                CancellationTokenSource cts = new CancellationTokenSource();
                return ParsePush.UnsubscribeAsync(new List<string> { { "test" } }, cts.Token);
            }).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsFalse(t.IsFaulted);
            });
        }
    }
}
