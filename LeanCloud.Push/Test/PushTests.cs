using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Storage.Internal;
using LeanCloud.Core.Internal;
using LeanCloud.Push.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class PushTests {
    private IAVPushController GetMockedPushController(IAVState expectedPushState) {
      Mock<IAVPushController> mockedController = new Mock<IAVPushController>(MockBehavior.Strict);

      mockedController.Setup(
        obj => obj.SendPushNotificationAsync(
          It.Is<IAVState>(s => s.Equals(expectedPushState)),
          It.IsAny<CancellationToken>()
        )
      ).Returns(Task.FromResult(false));

      return mockedController.Object;
    }

    private IAVPushChannelsController GetMockedPushChannelsController(IEnumerable<string> channels) {
      Mock<IAVPushChannelsController> mockedChannelsController = new Mock<IAVPushChannelsController>(MockBehavior.Strict);

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

    [TearDown]
    public void TearDown() {
      AVPlugins.Instance = null;
      AVPushPlugins.Instance = null;
    }

    [Test]
    [AsyncStateMachine(typeof(PushTests))]
    public Task TestSendPush() {
      MutableAVState state = new MutableAVState {
        Query = AVInstallation.Query
      };

      AVPush thePush = new AVPush();
      AVPushPlugins.Instance = new AVPushPlugins {
        PushController = GetMockedPushController(state)
      };

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

        AVQuery<AVInstallation> query = new AVQuery<AVInstallation>("aClass");
        thePush.Query = query;
        state.Query = query;

        return thePush.SendAsync();
      }).Unwrap().ContinueWith(t => {
        Assert.True(t.IsCompleted);
        Assert.False(t.IsFaulted);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(PushTests))]
    public Task TestSubscribe() {
      List<string> channels = new List<string>();
      AVPushPlugins.Instance = new AVPushPlugins {
        PushChannelsController = GetMockedPushChannelsController(channels)
      };

      channels.Add("test");
      return AVPush.SubscribeAsync("test").ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        return AVPush.SubscribeAsync(new List<string> { { "test" } });
      }).Unwrap().ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        CancellationTokenSource cts = new CancellationTokenSource();
        return AVPush.SubscribeAsync(new List<string> { { "test" } }, cts.Token);
      }).Unwrap().ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(PushTests))]
    public Task TestUnsubscribe() {
      List<string> channels = new List<string>();
      AVPushPlugins.Instance = new AVPushPlugins {
        PushChannelsController = GetMockedPushChannelsController(channels)
      };

      channels.Add("test");
      return AVPush.UnsubscribeAsync("test").ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        return AVPush.UnsubscribeAsync(new List<string> { { "test" } });
      }).ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);

        CancellationTokenSource cts = new CancellationTokenSource();
        return AVPush.UnsubscribeAsync(new List<string> { { "test" } }, cts.Token);
      }).ContinueWith(t => {
        Assert.IsTrue(t.IsCompleted);
        Assert.IsFalse(t.IsFaulted);
      });
    }
  }
}
