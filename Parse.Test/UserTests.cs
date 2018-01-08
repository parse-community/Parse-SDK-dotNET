using Moq;
using Parse;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class UserTests
    {
        [TestInitialize]
        public void SetUp()
        {
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();
        }

        [TestCleanup]
        public void TearDown() => ParseCorePlugins.Instance = null;

        [TestMethod]
        public void TestRemoveFields()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "name", "andrew" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            Assert.ThrowsException<ArgumentException>(() => user.Remove("username"));
            try { user.Remove("name"); }
            catch { Assert.Fail(); }
            Assert.IsFalse(user.ContainsKey("name"));
        }

        [TestMethod]
        public void TestSessionTokenGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            Assert.AreEqual("se551onT0k3n", user.SessionToken);
        }

        [TestMethod]
        public void TestUsernameGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            Assert.AreEqual("kevin", user.Username);
            user.Username = "ilya";
            Assert.AreEqual("ilya", user.Username);
        }

        [TestMethod]
        public void TestPasswordGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "password", "hurrah" },
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            Assert.AreEqual("hurrah", user.GetState()["password"]);
            user.Password = "david";
            Assert.IsNotNull(user.GetCurrentOperations()["password"]);
        }

        [TestMethod]
        public void TestEmailGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "email", "james@parse.com" },
          { "name", "andrew" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            Assert.AreEqual("james@parse.com", user.Email);
            user.Email = "bryan@parse.com";
            Assert.AreEqual("bryan@parse.com", user.Email);
        }

        [TestMethod]
        public void TestAuthDataGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "email", "james@parse.com" },
          { "authData", new Dictionary<string, object>() {
            { "facebook", new Dictionary<string, object>() {
              { "sessionToken", "none" }
            }}
          }}
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            Assert.AreEqual(1, user.GetAuthData().Count);
            Assert.IsInstanceOfType(user.GetAuthData()["facebook"], typeof (IDictionary<string, object>));
        }

        [TestMethod]
        public void TestGetUserQuery() => Assert.IsInstanceOfType(ParseUser.Query, typeof (ParseQuery<ParseUser>));

        [TestMethod]
        public void TestIsAuthenticated()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                CurrentUserController = mockCurrentUserController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            Assert.IsTrue(user.IsAuthenticated);
        }

        [TestMethod]
        public void TestIsAuthenticatedWithOtherParseUser()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            IObjectState state2 = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir2",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            ParseUser user2 = ParseObjectExtensions.FromState<ParseUser>(state2, "_User");
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                CurrentUserController = mockCurrentUserController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            Assert.IsFalse(user2.IsAuthenticated);
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestSignUpWithInvalidServerData()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");

            return user.SignUpAsync().ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof (InvalidOperationException));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestSignUp()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "username", "ihave" },
          { "password", "adream" }
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ObjectId = "some0neTol4v4"
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockController = new Mock<IParseUserController>();
            mockController.Setup(obj => obj.SignUpAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                UserController = mockController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return user.SignUpAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.SignUpAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.IsFalse(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.IsFalse(user.GetState().ContainsKey("password"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLogIn()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "username", "ihave" },
          { "password", "adream" }
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ObjectId = "some0neTol4v4"
            };
            var mockController = new Mock<IParseUserController>();
            mockController.Setup(obj => obj.LogInAsync("ihave",
                "adream",
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                UserController = mockController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return ParseUser.LogInAsync("ihave", "adream").ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.LogInAsync("ihave",
                  "adream",
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var user = t.Result;
                Assert.IsFalse(user.IsDirty);
                Assert.IsNull(user.Username);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestBecome()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            var mockController = new Mock<IParseUserController>();
            mockController.Setup(obj => obj.GetUserAsync("llaKcolnu", It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                UserController = mockController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return ParseUser.BecomeAsync("llaKcolnu").ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.GetUserAsync("llaKcolnu", It.IsAny<CancellationToken>()), Times.Exactly(1));

                var user = t.Result;
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("llaKcolnu", user.SessionToken);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLogOut()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "r:llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            var mockSessionController = new Mock<IParseSessionController>();
            mockSessionController.Setup(c => c.IsRevocableSessionToken(It.IsAny<string>())).Returns(true);

            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                CurrentUserController = mockCurrentUserController.Object,
                SessionController = mockSessionController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return ParseUser.LogOutAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockCurrentUserController.Verify(obj => obj.LogOutAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
                mockSessionController.Verify(obj => obj.RevokeAsync("r:llaKcolnu", It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        public void TestCurrentUser()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                CurrentUserController = mockCurrentUserController.Object,
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            Assert.AreEqual(user, ParseUser.CurrentUser);
        }

        [TestMethod]
        public void TestCurrentUserWithEmptyResult()
        {
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                CurrentUserController = mockCurrentUserController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            Assert.IsNull(ParseUser.CurrentUser);
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestRevocableSession()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "r:llaKcolnu" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockSessionController = new Mock<IParseSessionController>();
            mockSessionController.Setup(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu",
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                SessionController = mockSessionController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return user.UpgradeToRevocableSessionAsync(CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockSessionController.Verify(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu",
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.AreEqual("r:llaKcolnu", user.SessionToken);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestRequestPasswordReset()
        {
            var mockController = new Mock<IParseUserController>();
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                UserController = mockController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return ParseUser.RequestPasswordResetAsync("gogo@parse.com").ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.RequestPasswordResetAsync("gogo@parse.com",
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestUserSave()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "username", "ihave" },
          { "password", "adream" }
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "Alliance", "rekt" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = new Mock<IParseCurrentUserController>().Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();
            user["Alliance"] = "rekt";

            return user.SaveAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.IsFalse(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.IsFalse(user.GetState().ContainsKey("password"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("rekt", user["Alliance"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestUserFetch()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "username", "ihave" },
          { "password", "adream" }
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "Alliance", "rekt" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.FetchAsync(It.IsAny<IObjectState>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = new Mock<IParseCurrentUserController>().Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();
            user["Alliance"] = "rekt";

            return user.FetchAsync().ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockObjectController.Verify(obj => obj.FetchAsync(It.IsAny<IObjectState>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.IsTrue(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.IsTrue(user.GetState().ContainsKey("password"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("rekt", user["Alliance"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLink()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "garden", "ofWords" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = new Mock<IParseCurrentUserController>().Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return user.LinkWithAsync("parse", new Dictionary<string, object>(), CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.IsFalse(user.IsDirty);
                Assert.IsNotNull(user.GetAuthData());
                Assert.IsNotNull(user.GetAuthData()["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("ofWords", user["garden"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestUnlink()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "authData", new Dictionary<string, object> {
            { "parse", new Dictionary<string, object>() }
          }}
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "garden", "ofWords" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.IsCurrent(user)).Returns(true);
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return user.UnlinkFromAsync("parse", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.IsFalse(user.IsDirty);
                Assert.IsNotNull(user.GetAuthData());
                Assert.IsFalse(user.GetAuthData().ContainsKey("parse"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("ofWords", user["garden"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestUnlinkNonCurrentUser()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" },
          { "authData", new Dictionary<string, object> {
            { "parse", new Dictionary<string, object>() }
          }}
        }
            };
            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "garden", "ofWords" }
        }
            };
            ParseUser user = ParseObjectExtensions.FromState<ParseUser>(state, "_User");
            var mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            var mockCurrentUserController = new Mock<IParseCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.IsCurrent(user)).Returns(false);
            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return user.UnlinkFromAsync("parse", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.IsFalse(user.IsDirty);
                Assert.IsNotNull(user.GetAuthData());
                Assert.IsTrue(user.GetAuthData().ContainsKey("parse"));
                Assert.IsNull(user.GetAuthData()["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("ofWords", user["garden"]);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLogInWith()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            var mockController = new Mock<IParseUserController>();
            mockController.Setup(obj => obj.LogInAsync("parse",
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

            ParseCorePlugins.Instance = new ParseCorePlugins
            {
                UserController = mockController.Object
            };
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();

            return ParseUserExtensions.LogInWithAsync("parse", new Dictionary<string, object>(), CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                mockController.Verify(obj => obj.LogInAsync("parse",
                  It.IsAny<IDictionary<string, object>>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var user = t.Result;
                Assert.IsNotNull(user.GetAuthData());
                Assert.IsNotNull(user.GetAuthData()["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [TestMethod]
        public void TestImmutableKeys()
        {
            ParseUser user = new ParseUser();
            string[] immutableKeys = new string[] {
        "sessionToken", "isNew"
      };

            foreach (var key in immutableKeys)
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                  user[key] = "1234567890"
                );

                Assert.ThrowsException<InvalidOperationException>(() =>
                  user.Add(key, "1234567890")
                );

                Assert.ThrowsException<InvalidOperationException>(() =>
                  user.AddRangeUniqueToList(key, new string[] { "1234567890" })
                );

                Assert.ThrowsException<InvalidOperationException>(() =>
                  user.Remove(key)
                );

                Assert.ThrowsException<InvalidOperationException>(() =>
                  user.RemoveAllFromList(key, new string[] { "1234567890" })
                );
            }

            // Other special keys should be good
            user["username"] = "username";
            user["password"] = "password";
        }
    }
}
