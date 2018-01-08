using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse;
using Parse.Core.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Test
{
    [TestClass]
    public class SessionTests
    {
        [TestInitialize]
        public void SetUp()
        {
            ParseObject.RegisterSubclass<ParseSession>();
            ParseObject.RegisterSubclass<ParseUser>();
        }

        [TestCleanup]
        public void TearDown() => ParseCorePlugins.Instance.Reset();

        [TestMethod]
        public void TestGetSessionQuery() => Assert.IsInstanceOfType(ParseSession.Query, typeof(ParseQuery<ParseSession>));

        [TestMethod]
        public void TestGetSessionToken()
        {
            ParseSession session = ParseObjectExtensions.FromState<ParseSession>(new MutableObjectState { ServerData = new Dictionary<string, object>() { { "sessionToken", "llaKcolnu" } } }, "_Session");
            Assert.IsNotNull(session);
            Assert.AreEqual("llaKcolnu", session.SessionToken);
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestGetCurrentSession()
        {
            IObjectState sessionState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "newllaKcolnu" }
        }
            };
            var mockController = new Mock<IParseSessionController>();
            mockController.Setup(obj => obj.GetSessionAsync(It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionState));

            IObjectState userState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(userState, "_User");
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                SessionController = mockController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };
            ParseObject.RegisterSubclass<ParseSession>();

            return ParseSession.GetCurrentSessionAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.GetSessionAsync(It.Is<string>(sessionToken => sessionToken == "llaKcolnu"),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));

                var session = t.Result;
                Assert.AreEqual("newllaKcolnu", session.SessionToken);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestGetCurrentSessionWithNoCurrentUser()
        {
            var mockController = new Mock<IParseSessionController>();
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                SessionController = mockController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };

            return ParseSession.GetCurrentSessionAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsNull(t.Result);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestRevoke()
        {
            var mockController = new Mock<IParseSessionController>();
            mockController
              .Setup(sessionController => sessionController.IsRevocableSessionToken(It.IsAny<string>()))
              .Returns(true);

            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                SessionController = mockController.Object
            };

            CancellationTokenSource source = new CancellationTokenSource();
            return ParseSessionExtensions.RevokeAsync("r:someSession", source.Token).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.RevokeAsync(It.Is<string>(sessionToken => sessionToken == "r:someSession"),
                    source.Token), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(SessionTests))]
        public Task TestUpgradeToRevocableSession()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            var mockController = new Mock<IParseSessionController>();
            mockController.Setup(obj => obj.UpgradeToRevocableSessionAsync(It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                SessionController = mockController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            CancellationTokenSource source = new CancellationTokenSource();
            return ParseSessionExtensions.UpgradeToRevocableSessionAsync("someSession", source.Token).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.UpgradeToRevocableSessionAsync(It.Is<string>(sessionToken => sessionToken == "someSession"),
                    source.Token), Times.Exactly(1));

                Assert.AreEqual("llaKcolnu", t.Result);
            });
        }
    }
}
