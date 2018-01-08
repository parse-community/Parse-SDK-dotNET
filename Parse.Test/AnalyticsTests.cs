using Moq;
using Parse;
using Parse.Analytics.Internal;
using Parse.Core.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParseTest
{
    [TestClass]
    public class AnalyticsTests
    {
        [TestCleanup]
        public void TearDown() => ParseAnalyticsPlugins.Instance.Reset();

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsTests))]
        public Task TestTrackEvent()
        {
            var mockController = new Mock<IParseAnalyticsController>();
            var mockCorePlugins = new Mock<IParseCorePlugins>();
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();

            mockCorePlugins
              .Setup(corePlugins => corePlugins.CurrentUserController)
              .Returns(mockCurrentUserController.Object);

            mockCurrentUserController
              .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<CancellationToken>()))
              .Returns(Task.FromResult("sessionToken"));

            ParseAnalyticsPlugins plugins = new ParseAnalyticsPlugins();
            plugins.AnalyticsController = mockController.Object;
            plugins.CorePlugins = mockCorePlugins.Object;
            ParseAnalyticsPlugins.Instance = plugins;

            return ParseAnalytics.TrackEventAsync("SomeEvent").ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.TrackEventAsync(It.Is<string>(eventName => eventName == "SomeEvent"),
                    It.Is<IDictionary<string, string>>(dict => dict == null),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsTests))]
        public Task TestTrackEventWithDimension()
        {
            var mockController = new Mock<IParseAnalyticsController>();
            var mockCorePlugins = new Mock<IParseCorePlugins>();
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();

            mockCorePlugins
              .Setup(corePlugins => corePlugins.CurrentUserController)
              .Returns(mockCurrentUserController.Object);

            mockCurrentUserController
              .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<CancellationToken>()))
              .Returns(Task.FromResult("sessionToken"));

            ParseAnalyticsPlugins plugins = new ParseAnalyticsPlugins();
            plugins.AnalyticsController = mockController.Object;
            plugins.CorePlugins = mockCorePlugins.Object;
            ParseAnalyticsPlugins.Instance = plugins;

            var dimensions = new Dictionary<string, string>() {
        { "facebook", "hq" }
      };

            return ParseAnalytics.TrackEventAsync("SomeEvent", dimensions).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.TrackEventAsync(It.Is<string>(eventName => eventName == "SomeEvent"),
                    It.Is<IDictionary<string, string>>(dict => dict != null && dict.Count == 1),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsTests))]
        public Task TestTrackAppOpened()
        {
            var mockController = new Mock<IParseAnalyticsController>();
            var mockCorePlugins = new Mock<IParseCorePlugins>();
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();

            mockCorePlugins
              .Setup(corePlugins => corePlugins.CurrentUserController)
              .Returns(mockCurrentUserController.Object);

            mockCurrentUserController
              .Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<CancellationToken>()))
              .Returns(Task.FromResult("sessionToken"));

            ParseAnalyticsPlugins plugins = new ParseAnalyticsPlugins();
            plugins.AnalyticsController = mockController.Object;
            plugins.CorePlugins = mockCorePlugins.Object;
            ParseAnalyticsPlugins.Instance = plugins;

            return ParseAnalytics.TrackAppOpenedAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.TrackAppOpenedAsync(It.Is<string>(pushHash => pushHash == null),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }
    }
}
