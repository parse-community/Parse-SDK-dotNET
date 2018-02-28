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

namespace ParseTest
{
    [TestClass]
    public class AnalyticsControllerTests
    {
        [TestInitialize]
        public void SetUp() => ParseClient.Initialize(new ParseClient.Configuration { ApplicationId = "", WindowsKey = "" });

        [TestMethod]
        [AsyncStateMachine(typeof(AnalyticsControllerTests))]
        public Task TestTrackEventWithEmptyDimension()
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

        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
