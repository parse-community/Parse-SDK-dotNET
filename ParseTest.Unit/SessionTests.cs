using Parse;
using Parse.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class SessionTests {
    [SetUp]
    public void SetUp() {
      ParseObject.RegisterSubclass<ParseSession>();
      ParseObject.RegisterSubclass<ParseUser>();
    }

    [TearDown]
    public void TearDown() {
      ParseCorePlugins.Instance.SessionController = null;
      ParseCorePlugins.Instance.CurrentUserController = null;
      ParseObject.UnregisterSubclass<ParseSession>();
      ParseObject.UnregisterSubclass<ParseUser>();
    }

    [Test]
    public void TestGetSessionQuery() {
      Assert.IsInstanceOf<ParseQuery<ParseSession>>(ParseSession.Query);
    }

    [Test]
    public void TestGetSessionToken() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      ParseSession session = ParseObject.FromState<ParseSession>(state, "_Session");
      Assert.NotNull(session);
      Assert.AreEqual("llaKcolnu", session.SessionToken);
    }

    [Test]
    public void TestIsRevocableSessionToken() {
      Assert.True(ParseSession.IsRevocableSessionToken("r:session"));
      Assert.True(ParseSession.IsRevocableSessionToken("r:session:r:"));
      Assert.True(ParseSession.IsRevocableSessionToken("session:r:"));
      Assert.False(ParseSession.IsRevocableSessionToken("session:s:d:r"));
      Assert.False(ParseSession.IsRevocableSessionToken("s:ession:s:d:r"));
      Assert.False(ParseSession.IsRevocableSessionToken(""));
    }

    [Test]
    [AsyncStateMachine(typeof(SessionTests))]
    public Task TestGetCurrentSession() {
      IObjectState sessionState = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "newllaKcolnu" }
        }
      };
      var mockController = new Mock<IParseSessionController>();
      mockController.Setup(obj => obj.GetSessionAsync(It.IsAny<string>(),
          It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionState));

      IObjectState userState = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(userState, "_User");
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
          .Returns(Task.FromResult(user));

      ParseCorePlugins.Instance.SessionController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      return ParseSession.GetCurrentSessionAsync().ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockController.Verify(obj => obj.GetSessionAsync(It.Is<string>(sessionToken => sessionToken == "llaKcolnu"),
            It.IsAny<CancellationToken>()), Times.Exactly(1));

        var session = t.Result;
        Assert.AreEqual("newllaKcolnu", session.SessionToken);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionTests))]
    public Task TestGetCurrentSessionWithNoCurrentUser() {
      var mockController = new Mock<IParseSessionController>();
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      ParseCorePlugins.Instance.SessionController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      return ParseSession.GetCurrentSessionAsync().ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        Assert.Null(t.Result);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionTests))]
    public Task TestRevoke() {
      var mockController = new Mock<IParseSessionController>();
      ParseCorePlugins.Instance.SessionController = mockController.Object;

      CancellationTokenSource source = new CancellationTokenSource();
      return ParseSession.RevokeAsync("r:someSession", source.Token).ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockController.Verify(obj => obj.RevokeAsync(It.Is<string>(sessionToken => sessionToken == "r:someSession"),
            source.Token), Times.Exactly(1));
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionTests))]
    public Task TestUpgradeToRevocableSession() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      var mockController = new Mock<IParseSessionController>();
      mockController.Setup(obj => obj.UpgradeToRevocableSessionAsync(It.IsAny<string>(),
          It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      ParseCorePlugins.Instance.SessionController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      CancellationTokenSource source = new CancellationTokenSource();
      return ParseSession.UpgradeToRevocableSessionAsync("someSession", source.Token).ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockController.Verify(obj => obj.UpgradeToRevocableSessionAsync(It.Is<string>(sessionToken => sessionToken == "someSession"),
            source.Token), Times.Exactly(1));

        Assert.AreEqual("llaKcolnu", t.Result);
      });
    }
  }
}
