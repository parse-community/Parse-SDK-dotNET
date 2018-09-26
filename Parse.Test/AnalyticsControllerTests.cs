using Moq;
using Parse;
using Parse.Analytics.Internal;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class AnalyticsControllerTests
    {
        [TestInitialize]
        public void SetUp() => ParseClient.Initialize(new ParseClient.Configuration { ApplicationID = "", Key = "" });

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsControllerTests))]
        public Task TestTrackEventWithEmptyDimensions()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { }));

            return new ParseAnalyticsController(mockRunner.Object).TrackEventAsync("SomeEvent", dimensions: null, sessionToken: null, cancellationToken: CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Uri.AbsolutePath == "/1/events/SomeEvent"), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsControllerTests))]
        public Task TestTrackEventWithNonEmptyDimensions()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { }));

            Dictionary<string, string> dimensions = new Dictionary<string, string> { ["njwerjk12"] = "5523dd" };

            return new ParseAnalyticsController(mockRunner.Object).TrackEventAsync("SomeEvent", dimensions: dimensions, sessionToken: null, cancellationToken: CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Uri.AbsolutePath.Contains("/1/events/SomeEvent")), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsControllerTests))]
        public Task TestTrackAppOpenedWithEmptyPushHash()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>()));

            return new ParseAnalyticsController(mockRunner.Object).TrackAppOpenedAsync(null, sessionToken: null, cancellationToken: CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Uri.AbsolutePath == "/1/events/AppOpened"), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsControllerTests))]
        public Task TestTrackAppOpenedWithNonEmptyPushHash()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>()));

            string pushHash = "32j4hll12lkk";

            return new ParseAnalyticsController(mockRunner.Object).TrackAppOpenedAsync(pushHash, sessionToken: null, cancellationToken: CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
