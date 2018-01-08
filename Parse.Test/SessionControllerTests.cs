using Moq;
using Parse;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class SessionControllerTests
    {
        [TestInitialize]
        public void SetUp() => ParseClient.Initialize(new ParseClient.Configuration { ApplicationId = "", WindowsKey = "" });

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestGetSessionWithEmptyResult()
        {
            return new ParseSessionController(this.CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null)).Object).GetSessionAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestGetSession()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
                new Dictionary<string, object>() {
            { "__type", "Object" },
            { "className", "Session" },
            { "sessionToken", "S0m3Se551on" },
            { "restricted", true }
                });
            var mockRunner = CreateMockRunner(response);

            return new ParseSessionController(mockRunner.Object).GetSessionAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Uri.AbsolutePath == "/1/sessions/me"),
                  It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                  It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var session = t.Result;
                Assert.AreEqual(2, session.Count());
                Assert.IsTrue((bool)session["restricted"]);
                Assert.AreEqual("S0m3Se551on", session["sessionToken"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestRevoke()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
            var mockRunner = CreateMockRunner(response);

            var controller = new ParseSessionController(mockRunner.Object);
            return controller.RevokeAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Uri.AbsolutePath == "/1/logout"),
                  It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                  It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestUpgradeToRevocableSession()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
                new Dictionary<string, object>() {
            { "__type", "Object" },
            { "className", "Session" },
            { "sessionToken", "S0m3Se551on" },
            { "restricted", true }
                });
            var mockRunner = CreateMockRunner(response);

            var controller = new ParseSessionController(mockRunner.Object);
            return controller.UpgradeToRevocableSessionAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Uri.AbsolutePath == "/1/upgradeToRevocableSession"),
                  It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                  It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var session = t.Result;
                Assert.AreEqual(2, session.Count());
                Assert.IsTrue((bool)session["restricted"]);
                Assert.AreEqual("S0m3Se551on", session["sessionToken"]);
            });
        }

        [TestMethod]
        public void TestIsRevocableSessionToken()
        {
            IParseSessionController sessionController = new ParseSessionController(Mock.Of<IParseCommandRunner>());
            Assert.IsTrue(sessionController.IsRevocableSessionToken("r:session"));
            Assert.IsTrue(sessionController.IsRevocableSessionToken("r:session:r:"));
            Assert.IsTrue(sessionController.IsRevocableSessionToken("session:r:"));
            Assert.IsFalse(sessionController.IsRevocableSessionToken("session:s:d:r"));
            Assert.IsFalse(sessionController.IsRevocableSessionToken("s:ession:s:d:r"));
            Assert.IsFalse(sessionController.IsRevocableSessionToken(""));
        }


        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            var mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(),
                It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
