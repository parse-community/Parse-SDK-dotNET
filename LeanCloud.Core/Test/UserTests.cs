using LeanCloud;
using LeanCloud.Core.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace ParseTest
{
    [TestFixture]
    public class UserTests
    {
        [SetUp]
        public void SetUp()
        {
            string appId = ConfigurationManager.AppSettings["appId"];
            string appKey = ConfigurationManager.AppSettings["appKey"];
            AVClient.Initialize(appId, appKey);
        }

        [TearDown]
        public void TearDown()
        {
            AVPlugins.Instance = null;
        }

        [Test]
        public void TestRemoveFields()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "name", "andrew" }
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            Assert.Throws<ArgumentException>(() => user.Remove("username"));
            Assert.DoesNotThrow(() => user.Remove("name"));
            Assert.False(user.ContainsKey("name"));
        }

        [Test]
        public void TestSessionTokenGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            Assert.AreEqual("se551onT0k3n", user.SessionToken);
        }

        [Test]
        public void TestUsernameGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            Assert.AreEqual("kevin", user.Username);
            user.Username = "ilya";
            Assert.AreEqual("ilya", user.Username);
        }

        [Test]
        public void TestPasswordGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "password", "hurrah" },
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            Assert.AreEqual("hurrah", user.GetState()["password"]);
            user.Password = "david";
            Assert.NotNull(user.GetCurrentOperations()["password"]);
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            Assert.AreEqual("james@parse.com", user.Email);
            user.Email = "bryan@parse.com";
            Assert.AreEqual("bryan@parse.com", user.Email);
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            Assert.AreEqual(1, user.GetAuthData().Count);
            Assert.IsInstanceOf<IDictionary<string, object>>(user.GetAuthData()["facebook"]);
        }

        [Test]
        public void TestGetUserQuery()
        {
            Assert.IsInstanceOf<AVQuery<AVUser>>(AVUser.Query);
        }

        [Test]
        public void TestIsAuthenticated()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir",
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            AVPlugins.Instance = new AVPlugins
            {
                CurrentUserController = mockCurrentUserController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            Assert.True(user.IsAuthenticated);
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            AVUser user2 = AVObjectExtensions.FromState<AVUser>(state2, "_User");
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            AVPlugins.Instance = new AVPlugins
            {
                CurrentUserController = mockCurrentUserController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            Assert.False(user2.IsAuthenticated);
        }

        [Test]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestSignUpWithInvalidServerData()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");

            return user.SignUpAsync().ContinueWith(t =>
            {
                Assert.True(t.IsFaulted);
                Assert.IsInstanceOf<InvalidOperationException>(t.Exception.InnerException);
            });
        }

        [Test]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestSignUp()
        {
            var u = Utils.RandomUsername();
            var p = Utils.RandomString(10);
            AVUser user = new AVUser()
            {
                Username = u,
                Password = p
            };

            return user.SignUpAsync().ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                Assert.AreEqual(u, user.Username);
                Assert.False(user.GetState().ContainsKey("password"));
                Assert.IsNotNull(user.ObjectId);
            });
        }

        [Test]
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
            var mockController = new Mock<IAVUserController>();
            mockController.Setup(obj => obj.LogInAsync("ihave",
                "adream",
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            AVPlugins.Instance = new AVPlugins
            {
                UserController = mockController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return AVUser.LogInAsync("ihave", "adream").ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockController.Verify(obj => obj.LogInAsync("ihave",
                  "adream",
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var user = t.Result;
                Assert.False(user.IsDirty);
                Assert.Null(user.Username);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [Test]
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
            var mockController = new Mock<IAVUserController>();
            mockController.Setup(obj => obj.GetUserAsync("llaKcolnu",
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));
            AVPlugins.Instance = new AVPlugins
            {
                UserController = mockController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return AVUser.BecomeAsync("llaKcolnu").ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockController.Verify(obj => obj.GetUserAsync("llaKcolnu",
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var user = t.Result;
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("llaKcolnu", user.SessionToken);
            });
        }

        [Test]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLogOut()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "r:llaKcolnu" }
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            var mockSessionController = new Mock<IAVSessionController>();
            mockSessionController.Setup(c => c.IsRevocableSessionToken(It.IsAny<string>())).Returns(true);

            AVPlugins.Instance = new AVPlugins
            {
                CurrentUserController = mockCurrentUserController.Object,
                SessionController = mockSessionController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return AVUser.LogOutAsync().ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockCurrentUserController.Verify(obj => obj.LogOutAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
                mockSessionController.Verify(obj => obj.RevokeAsync("r:llaKcolnu", It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [Test]
        public void TestCurrentUser()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "sessionToken", "llaKcolnu" }
        }
            };
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(user));
            AVPlugins.Instance = new AVPlugins
            {
                CurrentUserController = mockCurrentUserController.Object,
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            Assert.AreEqual(user, AVUser.CurrentUser);
        }

        [Test]
        public void TestCurrentUserWithEmptyResult()
        {
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            AVPlugins.Instance = new AVPlugins
            {
                CurrentUserController = mockCurrentUserController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            Assert.Null(AVUser.CurrentUser);
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockSessionController = new Mock<IAVSessionController>();
            mockSessionController.Setup(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu",
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            AVPlugins.Instance = new AVPlugins
            {
                SessionController = mockSessionController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return user.UpgradeToRevocableSessionAsync(CancellationToken.None).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockSessionController.Verify(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu",
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.AreEqual("r:llaKcolnu", user.SessionToken);
            });
        }

        [Test]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestRequestPasswordReset()
        {
            var mockController = new Mock<IAVUserController>();
            AVPlugins.Instance = new AVPlugins
            {
                UserController = mockController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return AVUser.RequestPasswordResetAsync("gogo@parse.com").ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockController.Verify(obj => obj.RequestPasswordResetAsync("gogo@parse.com",
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockObjectController = new Mock<IAVObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            AVPlugins.Instance = new AVPlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = new Mock<IAVCurrentUserController>().Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();
            user["Alliance"] = "rekt";

            return user.SaveAsync().ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.False(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.False(user.GetState().ContainsKey("password"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("rekt", user["Alliance"]);
            });
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockObjectController = new Mock<IAVObjectController>();
            mockObjectController.Setup(obj => obj.FetchAsync(It.IsAny<IObjectState>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            AVPlugins.Instance = new AVPlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = new Mock<IAVCurrentUserController>().Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();
            user["Alliance"] = "rekt";

            return user.FetchAsync().ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockObjectController.Verify(obj => obj.FetchAsync(It.IsAny<IObjectState>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.True(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.True(user.GetState().ContainsKey("password"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("rekt", user["Alliance"]);
            });
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockObjectController = new Mock<IAVObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            AVPlugins.Instance = new AVPlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = new Mock<IAVCurrentUserController>().Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return user.LinkWithAsync("parse", new Dictionary<string, object>(), CancellationToken.None).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.False(user.IsDirty);
                Assert.NotNull(user.GetAuthData());
                Assert.NotNull(user.GetAuthData()["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("ofWords", user["garden"]);
            });
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockObjectController = new Mock<IAVObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.IsCurrent(user)).Returns(true);
            AVPlugins.Instance = new AVPlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return user.UnlinkFromAsync("parse", CancellationToken.None).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.False(user.IsDirty);
                Assert.NotNull(user.GetAuthData());
                Assert.False(user.GetAuthData().ContainsKey("parse"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("ofWords", user["garden"]);
            });
        }

        [Test]
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
            AVUser user = AVObjectExtensions.FromState<AVUser>(state, "_User");
            var mockObjectController = new Mock<IAVObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();
            mockCurrentUserController.Setup(obj => obj.IsCurrent(user)).Returns(false);
            AVPlugins.Instance = new AVPlugins
            {
                ObjectController = mockObjectController.Object,
                CurrentUserController = mockCurrentUserController.Object,
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return user.UnlinkFromAsync("parse", CancellationToken.None).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(),
                  It.IsAny<IDictionary<string, IAVFieldOperation>>(),
                  It.IsAny<string>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));
                Assert.False(user.IsDirty);
                Assert.NotNull(user.GetAuthData());
                Assert.True(user.GetAuthData().ContainsKey("parse"));
                Assert.Null(user.GetAuthData()["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
                Assert.AreEqual("ofWords", user["garden"]);
            });
        }

        [Test]
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
            var mockController = new Mock<IAVUserController>();
            mockController.Setup(obj => obj.LogInAsync("parse",
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

            AVPlugins.Instance = new AVPlugins
            {
                UserController = mockController.Object
            };
            AVObject.RegisterSubclass<AVUser>();
            AVObject.RegisterSubclass<AVSession>();

            return AVUserExtensions.LogInWithAsync("parse", new Dictionary<string, object>(), CancellationToken.None).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                mockController.Verify(obj => obj.LogInAsync("parse",
                  It.IsAny<IDictionary<string, object>>(),
                  It.IsAny<CancellationToken>()), Times.Exactly(1));

                var user = t.Result;
                Assert.NotNull(user.GetAuthData());
                Assert.NotNull(user.GetAuthData()["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [Test]
        public void TestImmutableKeys()
        {
            AVUser user = new AVUser();
            string[] immutableKeys = new string[] { "sessionToken", "isNew" };

            foreach (var key in immutableKeys)
            {
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

        [Test]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLogInByMobileWithPassword()
        {
            //var pwd = Utils.RandomString(10);
            //return Utils.SignUp(pwd).ContinueWith(_ =>
            // {
            //     var user = _.Result;
            //     return AVUser.LogInByMobilePhoneNumberAsync(user.MobilePhoneNumber, pwd).ContinueWith(t =>
            //     {
            //         Assert.False(t.IsFaulted);
            //         Assert.False(t.IsCanceled);
            //         Assert.IsNotNull(user.ObjectId);
            //     });
            // });

            return AVUser.LogInByMobilePhoneNumberAsync("15601502941", "leancloud").ContinueWith(t =>
            {
                var u = t.Result;
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                Assert.IsNotNull(u.ObjectId);
                return Task.FromResult(0);
            });
        }
    }
}
