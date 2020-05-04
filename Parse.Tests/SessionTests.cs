using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests
{
    [TestClass]
    public class SessionTests
    {
        ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

        [TestInitialize]
        public void SetUp()
        {
            Client.AddValidClass<ParseSession>();
            Client.AddValidClass<ParseUser>();
        }

        [TestCleanup]
        public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestGetSessionQuery() => Assert.IsInstanceOfType(Client.GetSessionQuery(), typeof(ParseQuery<ParseSession>));

        [TestMethod]
        public void TestGetSessionToken()
        {
            ParseSession session = Client.GenerateObjectFromState<ParseSession>(new MutableObjectState { ServerData = new Dictionary<string, object>() { ["sessionToken"] = "llaKcolnu" } }, "_Session");

            Assert.IsNotNull(session);
            Assert.AreEqual("llaKcolnu", session.SessionToken);
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestGetCurrentSession()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            IObjectState sessionState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "newllaKcolnu"
                }
            };

            Mock<IParseSessionController> mockController = new Mock<IParseSessionController>();
            mockController.Setup(obj => obj.GetSessionAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionState));

            IObjectState userState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            ParseUser user = client.GenerateObjectFromState<ParseUser>(userState, "_User");

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(user));

            hub.SessionController = mockController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return client.GetCurrentSessionAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.GetSessionAsync(It.Is<string>(sessionToken => sessionToken == "llaKcolnu"), It.IsAny<IServiceHub>(),It.IsAny<CancellationToken>()), Times.Exactly(1));

                ParseSession session = task.Result;
                Assert.AreEqual("newllaKcolnu", session.SessionToken);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestGetCurrentSessionWithNoCurrentUser()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseSessionController> mockController = new Mock<IParseSessionController>();
            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController>();

            hub.SessionController = mockController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return client.GetCurrentSessionAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsNull(task.Result);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestRevoke()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseSessionController> mockController = new Mock<IParseSessionController>();
            mockController.Setup(sessionController => sessionController.IsRevocableSessionToken(It.IsAny<string>())).Returns(true);

            hub.SessionController = mockController.Object;

            CancellationTokenSource source = new CancellationTokenSource { };
            return client.RevokeSessionAsync("r:someSession", source.Token).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.RevokeAsync(It.Is<string>(sessionToken => sessionToken == "r:someSession"), source.Token), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestUpgradeToRevocableSession()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>()
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            Mock<IParseSessionController> mockController = new Mock<IParseSessionController>();
            mockController.Setup(obj => obj.UpgradeToRevocableSessionAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController>();

            hub.SessionController = mockController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            CancellationTokenSource source = new CancellationTokenSource { };
            return client.UpgradeToRevocableSessionAsync("someSession", source.Token).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.UpgradeToRevocableSessionAsync(It.Is<string>(sessionToken => sessionToken == "someSession"), It.IsAny<IServiceHub>(), source.Token), Times.Exactly(1));

                Assert.AreEqual("llaKcolnu", task.Result);
            });
        }
    }
}
