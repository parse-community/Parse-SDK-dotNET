using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;
using Parse.Abstractions.Platform.Analytics;
using Parse.Abstractions.Platform.Users;

namespace Parse.Tests
{
    [TestClass]
    public class AnalyticsTests
    {
#warning Skipped post-test-evaluation cleaning method may be needed.

        // [TestCleanup]
        // public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsTests))]
        public Task TestTrackEvent()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseAnalyticsController> mockController = new Mock<IParseAnalyticsController> { };
            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };

            mockCurrentUserController.Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("sessionToken"));

            hub.AnalyticsController = mockController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return client.TrackAnalyticsEventAsync("SomeEvent").ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.TrackEventAsync(It.Is<string>(eventName => eventName == "SomeEvent"), It.Is<IDictionary<string, string>>(dict => dict == null), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsTests))]
        public Task TestTrackEventWithDimension()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseAnalyticsController> mockController = new Mock<IParseAnalyticsController> { };
            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };

            mockCurrentUserController.Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("sessionToken"));

            hub.AnalyticsController = mockController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return client.TrackAnalyticsEventAsync("SomeEvent", new Dictionary<string, string> { ["facebook"] = "hq" }).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                mockController.Verify(obj => obj.TrackEventAsync(It.Is<string>(eventName => eventName == "SomeEvent"), It.Is<IDictionary<string, string>>(dict => dict != null && dict.Count == 1), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsTests))]
        public Task TestTrackAppOpened()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseAnalyticsController> mockController = new Mock<IParseAnalyticsController> { };
            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };

            mockCurrentUserController.Setup(controller => controller.GetCurrentSessionTokenAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("sessionToken"));

            hub.AnalyticsController = mockController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return client.TrackLaunchAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.TrackAppOpenedAsync(It.Is<string>(pushHash => pushHash == null), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }
    }
}
