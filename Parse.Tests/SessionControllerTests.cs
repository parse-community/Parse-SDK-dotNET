using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Sessions;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Sessions;

namespace Parse.Tests
{
    [TestClass]
    public class SessionControllerTests
    {
#warning Check if reinitializing the client for every test method is really necessary.

        ParseClient Client { get; set; }

        [TestInitialize]
        public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestGetSessionWithEmptyResult() => new ParseSessionController(CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null)).Object, Client.Decoder).GetSessionAsync("S0m3Se551on", Client, CancellationToken.None).ContinueWith(task =>
        {
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
        });

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestGetSession()
        {
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "Session",
                ["sessionToken"] = "S0m3Se551on",
                ["restricted"] = true
            });

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);

            return new ParseSessionController(mockRunner.Object, Client.Decoder).GetSessionAsync("S0m3Se551on", Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "sessions/me"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState session = task.Result;
                Assert.AreEqual(2, session.Count());
                Assert.IsTrue((bool) session["restricted"]);
                Assert.AreEqual("S0m3Se551on", session["sessionToken"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestRevoke()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, default));

            return new ParseSessionController(mockRunner.Object, Client.Decoder).RevokeAsync("S0m3Se551on", CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "logout"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionControllerTests))]
        public Task TestUpgradeToRevocableSession()
        {
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
                new Dictionary<string, object>
                {
                    ["__type"] = "Object",
                    ["className"] = "Session",
                    ["sessionToken"] = "S0m3Se551on",
                    ["restricted"] = true
                });

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);

            return new ParseSessionController(mockRunner.Object, Client.Decoder).UpgradeToRevocableSessionAsync("S0m3Se551on", Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "upgradeToRevocableSession"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState session = task.Result;
                Assert.AreEqual(2, session.Count());
                Assert.IsTrue((bool) session["restricted"]);
                Assert.AreEqual("S0m3Se551on", session["sessionToken"]);
            });
        }

        [TestMethod]
        public void TestIsRevocableSessionToken()
        {
            IParseSessionController sessionController = new ParseSessionController(Mock.Of<IParseCommandRunner>(), Client.Decoder);
            Assert.IsTrue(sessionController.IsRevocableSessionToken("r:session"));
            Assert.IsTrue(sessionController.IsRevocableSessionToken("r:session:r:"));
            Assert.IsTrue(sessionController.IsRevocableSessionToken("session:r:"));
            Assert.IsFalse(sessionController.IsRevocableSessionToken("session:s:d:r"));
            Assert.IsFalse(sessionController.IsRevocableSessionToken("s:ession:s:d:r"));
            Assert.IsFalse(sessionController.IsRevocableSessionToken(""));
        }


        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
