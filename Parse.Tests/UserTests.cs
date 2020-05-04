using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Platform.Objects;

namespace Parse.Tests
{
#warning Class refactoring requires completion.

    [TestClass]
    public class UserTests
    {
        ParseClient Client { get; set; } = new ParseClient(new ServerConnectionData { Test = true });

        [TestCleanup]
        public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestRemoveFields()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                    ["name"] = "andrew"
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");
            Assert.ThrowsException<InvalidOperationException>(() => user.Remove("username"));

            try
            {
                user.Remove("name");
            }
            catch
            {
                Assert.Fail(@"Removing ""name"" field on ParseUser should not throw an exception because ""name"" is not an immutable field and was defined on the object.");
            }

            Assert.IsFalse(user.ContainsKey("name"));
        }

        [TestMethod]
        public void TestSessionTokenGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");
            Assert.AreEqual("se551onT0k3n", user.SessionToken);
        }

        [TestMethod]
        public void TestUsernameGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");
            Assert.AreEqual("kevin", user.Username);
            user.Username = "ilya";
            Assert.AreEqual("ilya", user.Username);
        }

        [TestMethod]
        public void TestPasswordGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                    ["password"] = "hurrah"
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");
            Assert.AreEqual("hurrah", user.State["password"]);
            user.Password = "david";
            Assert.IsNotNull(user.CurrentOperations["password"]);
        }

        [TestMethod]
        public void TestEmailGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["email"] = "james@parse.com",
                    ["name"] = "andrew",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");
            Assert.AreEqual("james@parse.com", user.Email);
            user.Email = "bryan@parse.com";
            Assert.AreEqual("bryan@parse.com", user.Email);
        }

        [TestMethod]
        public void TestAuthDataGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["email"] = "james@parse.com",
                    ["authData"] = new Dictionary<string, object>
                    {
                        ["facebook"] = new Dictionary<string, object>
                        {
                            ["sessionToken"] = "none"
                        }
                    }
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");
            Assert.AreEqual(1, user.AuthData.Count);
            Assert.IsInstanceOfType(user.AuthData["facebook"], typeof(IDictionary<string, object>));
        }

        [TestMethod]
        public void TestGetUserQuery() => Assert.IsInstanceOfType(Client.GetUserQuery(), typeof(ParseQuery<ParseUser>));

        [TestMethod]
        public void TestIsAuthenticated()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir",
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(user));

            hub.CurrentUserController = mockCurrentUserController.Object;

            Assert.IsTrue(user.IsAuthenticated);
        }

        [TestMethod]
        public void TestIsAuthenticatedWithOtherParseUser()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir",
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            IObjectState state2 = new MutableObjectState
            {
                ObjectId = "wagimanPutraPetir2",
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");
            ParseUser user2 = client.GenerateObjectFromState<ParseUser>(state2, "_User");

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(user));

            hub.CurrentUserController = mockCurrentUserController.Object;

            Assert.IsFalse(user2.IsAuthenticated);
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestSignUpWithInvalidServerData()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            ParseUser user = Client.GenerateObjectFromState<ParseUser>(state, "_User");

            return user.SignUpAsync().ContinueWith(task =>
            {
                Assert.IsTrue(task.IsFaulted);
                Assert.IsInstanceOfType(task.Exception.InnerException, typeof(InvalidOperationException));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestSignUp()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu",
                    ["username"] = "ihave",
                    ["password"] = "adream"
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ObjectId = "some0neTol4v4"
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseUserController> mockController = new Mock<IParseUserController> { };
            mockController.Setup(obj => obj.SignUpAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            hub.UserController = mockController.Object;

            return user.SignUpAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.SignUpAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.IsFalse(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.IsFalse(user.State.ContainsKey("password"));
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestLogIn()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu",
                    ["username"] = "ihave",
                    ["password"] = "adream"
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ObjectId = "some0neTol4v4"
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseUserController> mockController = new Mock<IParseUserController> { };
            mockController.Setup(obj => obj.LogInAsync("ihave", "adream", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            hub.UserController = mockController.Object;

            return client.LogInAsync("ihave", "adream").ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.LogInAsync("ihave", "adream", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                ParseUser user = task.Result;
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
                ServerData = new Dictionary<string, object> { ["sessionToken"] = "llaKcolnu" }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseUserController> mockController = new Mock<IParseUserController> { };
            mockController.Setup(obj => obj.GetUserAsync("llaKcolnu", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

            hub.UserController = mockController.Object;

            return client.BecomeAsync("llaKcolnu").ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.GetUserAsync("llaKcolnu", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                ParseUser user = task.Result;
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
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "r:llaKcolnu"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(user));

            Mock<IParseSessionController> mockSessionController = new Mock<IParseSessionController>();
            mockSessionController.Setup(c => c.IsRevocableSessionToken(It.IsAny<string>())).Returns(true);

            hub.CurrentUserController = mockCurrentUserController.Object;
            hub.SessionController = mockSessionController.Object;

            return client.LogOutAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockCurrentUserController.Verify(obj => obj.LogOutAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
                mockSessionController.Verify(obj => obj.RevokeAsync("r:llaKcolnu", It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        public void TestCurrentUser()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };
            mockCurrentUserController.Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(user));

            hub.CurrentUserController = mockCurrentUserController.Object;

            Assert.AreEqual(user, client.GetCurrentUser());
        }

        [TestMethod]
        public void TestCurrentUserWithEmptyResult() => Assert.IsNull(new ParseClient(new ServerConnectionData { Test = true }, new MutableServiceHub { CurrentUserController = new Mock<IParseCurrentUserController> { }.Object }).GetCurrentUser());

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestRevocableSession()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "r:llaKcolnu"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseSessionController> mockSessionController = new Mock<IParseSessionController>();
            mockSessionController.Setup(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            hub.SessionController = mockSessionController.Object;

            return user.UpgradeToRevocableSessionAsync(CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockSessionController.Verify(obj => obj.UpgradeToRevocableSessionAsync("llaKcolnu", It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.AreEqual("r:llaKcolnu", user.SessionToken);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestRequestPasswordReset()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseUserController> mockController = new Mock<IParseUserController> { };

            hub.UserController = mockController.Object;

            return client.RequestPasswordResetAsync("gogo@parse.com").ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.RequestPasswordResetAsync("gogo@parse.com", It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserTests))]
        public Task TestUserSave()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "some0neTol4v4",
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu",
                    ["username"] = "ihave",
                    ["password"] = "adream"
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["Alliance"] = "rekt"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");
            Mock<IParseObjectController> mockObjectController = new Mock<IParseObjectController>();

            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            hub.ObjectController = mockObjectController.Object;
            hub.CurrentUserController = new Mock<IParseCurrentUserController> { }.Object;

            user["Alliance"] = "rekt";

            return user.SaveAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.IsFalse(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.IsFalse(user.State.ContainsKey("password"));
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
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu",
                    ["username"] = "ihave",
                    ["password"] = "adream"
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["Alliance"] = "rekt"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseObjectController> mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.FetchAsync(It.IsAny<IObjectState>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            hub.ObjectController = mockObjectController.Object;
            hub.CurrentUserController = new Mock<IParseCurrentUserController> { }.Object;

            user["Alliance"] = "rekt";

            return user.FetchAsync().ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockObjectController.Verify(obj => obj.FetchAsync(It.IsAny<IObjectState>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.IsTrue(user.IsDirty);
                Assert.AreEqual("ihave", user.Username);
                Assert.IsTrue(user.State.ContainsKey("password"));
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
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["garden"] = "ofWords"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseObjectController> mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            hub.ObjectController = mockObjectController.Object;
            hub.CurrentUserController = new Mock<IParseCurrentUserController> { }.Object;

            return user.LinkWithAsync("parse", new Dictionary<string, object> { }, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.IsFalse(user.IsDirty);
                Assert.IsNotNull(user.AuthData);
                Assert.IsNotNull(user.AuthData["parse"]);
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
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu",
                    ["authData"] = new Dictionary<string, object>
                    {
                        ["parse"] = new Dictionary<string, object> { }
                    }
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["garden"] = "ofWords"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseObjectController> mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };
            mockCurrentUserController.Setup(obj => obj.IsCurrent(user)).Returns(true);

            hub.ObjectController = mockObjectController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return user.UnlinkFromAsync("parse", CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.IsFalse(user.IsDirty);
                Assert.IsNotNull(user.AuthData);
                Assert.IsFalse(user.AuthData.ContainsKey("parse"));
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
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu",
                    ["authData"] = new Dictionary<string, object>
                    {
                        ["parse"] = new Dictionary<string, object> { }
                    }
                }
            };

            IObjectState newState = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>
                {
                    ["garden"] = "ofWords"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            ParseUser user = client.GenerateObjectFromState<ParseUser>(state, "_User");

            Mock<IParseObjectController> mockObjectController = new Mock<IParseObjectController>();
            mockObjectController.Setup(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(newState));

            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController> { };
            mockCurrentUserController.Setup(obj => obj.IsCurrent(user)).Returns(false);

            hub.ObjectController = mockObjectController.Object;
            hub.CurrentUserController = mockCurrentUserController.Object;

            return user.UnlinkFromAsync("parse", CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockObjectController.Verify(obj => obj.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                Assert.IsFalse(user.IsDirty);
                Assert.IsNotNull(user.AuthData);
                Assert.IsTrue(user.AuthData.ContainsKey("parse"));
                Assert.IsNull(user.AuthData["parse"]);
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
                ServerData = new Dictionary<string, object>
                {
                    ["sessionToken"] = "llaKcolnu"
                }
            };

            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseUserController> mockController = new Mock<IParseUserController> { };
            mockController.Setup(obj => obj.LogInAsync("parse", It.IsAny<IDictionary<string, object>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(state));

            hub.UserController = mockController.Object;

            return client.LogInWithAsync("parse", new Dictionary<string, object> { }, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockController.Verify(obj => obj.LogInAsync("parse", It.IsAny<IDictionary<string, object>>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                ParseUser user = task.Result;

                Assert.IsNotNull(user.AuthData);
                Assert.IsNotNull(user.AuthData["parse"]);
                Assert.AreEqual("some0neTol4v4", user.ObjectId);
            });
        }

        [TestMethod]
        public void TestImmutableKeys()
        {
            ParseUser user = new ParseUser { }.Bind(Client) as ParseUser;
            string[] immutableKeys = new string[] { "sessionToken", "isNew" };

            foreach (string key in immutableKeys)
            {
                Assert.ThrowsException<InvalidOperationException>(() => user[key] = "1234567890");

                Assert.ThrowsException<InvalidOperationException>(() => user.Add(key, "1234567890"));

                Assert.ThrowsException<InvalidOperationException>(() => user.AddRangeUniqueToList(key, new string[] { "1234567890" }));

                Assert.ThrowsException<InvalidOperationException>(() => user.Remove(key));

                Assert.ThrowsException<InvalidOperationException>(() => user.RemoveAllFromList(key, new string[] { "1234567890" }));
            }

            // Other special keys should be good.

            user["username"] = "username";
            user["password"] = "password";
        }
    }
}
