using Moq;
using NUnit.Framework;
using Parse;
using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class AnalyticsTests {
    [TearDown]
    public void TearDown() {
      ParseCorePlugins.Instance.AnalyticsController = null;
      ParseCorePlugins.Instance.CurrentUserController = null;
    }

    [Test]
    [AsyncStateMachine(typeof(AnalyticsTests))]
    public Task TestTrackEvent() {
      var mockController = new Mock<IParseAnalyticsController>();
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      ParseCorePlugins.Instance.AnalyticsController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      return ParseAnalytics.TrackEventAsync("SomeEvent").ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        mockController.Verify(obj => obj.TrackEventAsync(It.Is<string>(eventName => eventName == "SomeEvent"),
            It.Is<IDictionary<string, string>>(dict => dict == null),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }

    [Test]
    [AsyncStateMachine(typeof(AnalyticsTests))]
    public Task TestTrackEventWithDimension() {
      var mockController = new Mock<IParseAnalyticsController>();
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      var dimensions = new Dictionary<string, string>() {
        { "facebook", "hq" }
      };
      ParseCorePlugins.Instance.AnalyticsController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      return ParseAnalytics.TrackEventAsync("SomeEvent", dimensions).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        mockController.Verify(obj => obj.TrackEventAsync(It.Is<string>(eventName => eventName == "SomeEvent"),
            It.Is<IDictionary<string, string>>(dict => dict != null && dict.Count == 1),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }

    [Test]
    [AsyncStateMachine(typeof(AnalyticsTests))]
    public Task TestTrackAppOpened() {
      var mockController = new Mock<IParseAnalyticsController>();
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      ParseCorePlugins.Instance.AnalyticsController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      return ParseAnalytics.TrackAppOpenedAsync().ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        mockController.Verify(obj => obj.TrackAppOpenedAsync(It.Is<string>(pushHash => pushHash == null),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }
  }
}
