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
  public class UserTests {
    [SetUp]
    public void SetUp() {
      ParseObject.RegisterSubclass<ParseUser>();
      ParseObject.RegisterSubclass<ParseSession>();
    }

    [TearDown]
    public void TearDown() {
      ParseCorePlugins.Instance.UserController = null;
      ParseCorePlugins.Instance.CurrentUserController = null;
      ParseCorePlugins.Instance.SessionController = null;
      ParseCorePlugins.Instance.ObjectController = null;
      ParseObject.UnregisterSubclass("_User");
      ParseObject.UnregisterSubclass("_Session");
    }

    [Test]
    public void TestRemoveFields() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "name", "andrew" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      Assert.Throws<ArgumentException>(() => user.Remove("username"));
      Assert.DoesNotThrow(() => user.Remove("name"));
      Assert.False(user.ContainsKey("name"));
    }

    [Test]
    public void TestSessionTokenGetter() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      Assert.AreEqual("se551onT0k3n", user.SessionToken);
    }

    [Test]
    public void TestUsernameGetterSetter() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      Assert.AreEqual("kevin", user.Username);
      user.Username = "ilya";
      Assert.AreEqual("ilya", user.Username);
    }

    [Test]
    public void TestPasswordGetterSetter() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "password", "hurrah" },
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      Assert.AreEqual("hurrah", user.State["password"]);
      user.Password = "david";
      Assert.NotNull(user.CurrentOperations["password"]);
    }

    [Test]
    public void TestEmailGetterSetter() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "email", "james@parse.com" },
          { "name", "andrew" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      Assert.AreEqual("james@parse.com", user.Email);
      user.Email = "bryan@parse.com";
      Assert.AreEqual("bryan@parse.com", user.Email);
    }

    [Test]
    public void TestAuthDataGetter() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "email", "james@parse.com" },
          { "authData", new Dictionary<string, object>() {
            { "facebook", new Dictionary<string, object>() {
              { "sessionToken", "none" }
            }}
          }}
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      Assert.AreEqual(1, user.AuthData.Count);
      Assert.IsInstanceOf<IDictionary<string, object>>(user.AuthData["facebook"]);
    }

    [Test]
    public void TestGetUserQuery() {
      Assert.IsInstanceOf<ParseQuery<ParseUser>>(ParseUser.Query);
    }

    [Test]
    public void TestIsAuthenticated() {
      IObjectState state = new MutableObjectState {
        ObjectId = "wagimanPutraPetir",
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
          .Returns(Task.FromResult(user));
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      Assert.True(user.IsAuthenticated);
    }

    [Test]
    public void TestIsAuthenticatedWithOtherParseUser() {
      IObjectState state = new MutableObjectState {
        ObjectId = "wagimanPutraPetir",
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      IObjectState state2 = new MutableObjectState {
        ObjectId = "wagimanPutraPetir2",
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      ParseUser user2 = ParseObject.FromState<ParseUser>(state2, "_User");
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
          .Returns(Task.FromResult(user));
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      Assert.False(user2.IsAuthenticated);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestSignUpWithInvalidServerData() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");

      return user.SignUpAsync().ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.IsInstanceOf<InvalidOperationException>(t.Exception.InnerException);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestSignUp() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "username", "ihave" },
          { "password", "adream" }
        }
      };
      IObjectState newState = new MutableObjectState {
        ObjectId = "some0neTol4v4"
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      var mockController = new Mock<IParseUserController>();
      mockController.Setup(obj => obj.SignUpAsync(It.IsAny<IObjectState>(),
          It.IsAny<IDictionary<string, IParseFieldOperation>>(),
          It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
      ParseCorePlugins.Instance.UserController = mockController.Object;

      return user.SignUpAsync().ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockController.Verify(obj => obj.SignUpAsync(It.IsAny<IObjectState>(),
          It.IsAny<IDictionary<string, IParseFieldOperation>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));
        Assert.False(user.IsDirty);
        Assert.AreEqual("ihave", user.Username);
        Assert.False(user.State.ContainsKey("password"));
        Assert.AreEqual("some0neTol4v4", user.ObjectId);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestLogIn() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestBecome() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestLogOut() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "r:llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
          .Returns(Task.FromResult(user));
      var mockSessionController = new Mock<IParseSessionController>();
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;
      ParseCorePlugins.Instance.SessionController = mockSessionController.Object;

      return ParseUser.LogOutAsync().ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockCurrentUserController.Verify(obj => obj.LogOutAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
        mockSessionController.Verify(obj => obj.RevokeAsync("r:llaKcolnu", It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }

    [Test]
    public void TestCurrentUser() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
          .Returns(Task.FromResult(user));
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      Assert.AreEqual(user, ParseUser.CurrentUser);
    }

    [Test]
    public void TestCurrentUserWithEmptyResult() {
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      Assert.Null(ParseUser.CurrentUser);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestRevocableSession() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
      };
      IObjectState newState = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "sessionToken", "r:llaKcolnu" }
        }
      };
      ParseUser user = ParseObject.FromState<ParseUser>(state, "_User");
      var mockSessionController = new Mock<IParseSessionController>();
      mockSessionController.Setup(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu",
          It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
      ParseCorePlugins.Instance.SessionController = mockSessionController.Object;

      return user.UpgradeToRevocableSessionAsync().ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockSessionController.Verify(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu",
            It.IsAny<CancellationToken>()), Times.Exactly(1));
        Assert.AreEqual("r:llaKcolnu", user.SessionToken);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestRequestPasswordReset() {
      var mockController = new Mock<IParseUserController>();
      ParseCorePlugins.Instance.UserController = mockController.Object;

      return ParseUser.RequestPasswordResetAsync("gogo@parse.com").ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockController.Verify(obj => obj.RequestPasswordResetAsync("gogo@parse.com",
            It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }


    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestUserSave() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestUserFetch() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestLink() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestUnlink() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(UserTests))]
    public Task TestLogInWith() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    public void TestImmutableKeys() {
      ParseUser user = new ParseUser();
      string[] immutableKeys = new string[] {
        "sessionToken", "isNew"
      };

      foreach (var key in immutableKeys) {
        Assert.Throws<InvalidOperationException>(() =>
          user[key] = "1234567890"
        );

        Assert.Throws<InvalidOperationException>(() =>
          user.Add(key, "1234567890")
        );

        Assert.Throws<InvalidOperationException>(() =>
          user.AddRangeUniqueToList(key, new string[] { "1234567890" })
        );

        Assert.Throws<InvalidOperationException>(() =>
          user.Remove(key)
        );

        Assert.Throws<InvalidOperationException>(() =>
          user.RemoveAllFromList(key, new string[] { "1234567890" })
        );
      }

      // Other special keys should be good
      user["username"] = "username";
      user["password"] = "password";
    }
  }
}
