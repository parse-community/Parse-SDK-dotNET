using Moq;
using NUnit.Framework;
using Parse;
using Parse.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class PushTests {
    private IParsePushController GetMockedPushController(IPushState expectedPushState) {
      Mock<IParsePushController> mockedController = new Mock<IParsePushController>(MockBehavior.Strict);

      mockedController.Setup(
        obj => obj.SendPushNotificationAsync(
          It.Is<IPushState>(s => s.Equals(expectedPushState)),
          It.IsAny<string>(),
          It.IsAny<CancellationToken>()
        )
      ).Returns(Task.FromResult(false));

      return mockedController.Object;
    }

    private IParsePushChannelsController GetMockedPushChannelsController(IEnumerable<string> channels) {
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

    [Test]
    [AsyncStateMachine(typeof(PushTests))]
    public Task TestSendPush() {
      MutablePushState state = new MutablePushState {
        Query = ParseInstallation.Query
      };

      ParsePush thePush = new ParsePush();
      ParseCorePlugins.Instance.PushController = GetMockedPushController(state);

      thePush.Alert = "Alert";
      state.Alert = "Alert";

      return thePush.SendAsync().ContinueWith(t => {
        Assert.True(t.IsCompleted);
        Assert.False(t.IsFaulted);

        thePush.Channels = new List<string> { { "channel" } };
        state.Channels = new List<string> { { "channel" } };

        return thePush.SendAsync();
      }).Unwrap().ContinueWith(t => {
        Assert.True(t.IsCompleted);
        Assert.False(t.IsFaulted);

        ParseQuery<ParseInstallation> query = new ParseQuery<ParseInstallation>("aClass");
        thePush.Query = query;
        state.Query = query;

        return thePush.SendAsync();
      }).Unwrap().ContinueWith(t => {
        Assert.True(t.IsCompleted);
        Assert.False(t.IsFaulted);

        ParseCorePlugins.Instance.PushController = null;
      });
    }

    [Test]
    [AsyncStateMachine(typeof(PushTests))]
    public Task TestSubscribe() {
      List<string> channels = new List<string>();
      ParseCorePlugins.Instance.PushChannelsController = GetMockedPushChannelsController(channels);

      channels.Add("test");
      return ParsePush.SubscribeAsync("test").ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        return ParsePush.SubscribeAsync(new List<string> { { "test" } });
      }).Unwrap().ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        CancellationTokenSource cts = new CancellationTokenSource();
        return ParsePush.SubscribeAsync(new List<string> { { "test" } }, cts.Token);
      }).Unwrap().ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        ParseCorePlugins.Instance.PushChannelsController = null;
      });
    }

    [Test]
    [AsyncStateMachine(typeof(PushTests))]
    public Task TestUnsubscribe() {
      List<string> channels = new List<string>();
      ParseCorePlugins.Instance.PushChannelsController = GetMockedPushChannelsController(channels);

      channels.Add("test");
      return ParsePush.UnsubscribeAsync("test").ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        return ParsePush.UnsubscribeAsync(new List<string> { { "test" } });
      }).ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        CancellationTokenSource cts = new CancellationTokenSource();
        return ParsePush.UnsubscribeAsync(new List<string> { { "test" } }, cts.Token);
      }).ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        ParseCorePlugins.Instance.PushChannelsController = null;
      });
    }
  }
}
