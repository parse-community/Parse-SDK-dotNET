using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;
using Parse.Platform.Users;

namespace Parse.Tests
{
    [TestClass]
    public class UserControllerTests
    {
        ParseClient Client { get; set; }

        [TestInitialize]
        public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

        [TestMethod]
        [AsyncStateMachine(typeof(UserControllerTests))]
        public Task TestSignUp()
        {
            MutableObjectState state = new MutableObjectState
            {
                ClassName = "_User",
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "hallucinogen",
                    ["password"] = "secret"
                }
            };

            Dictionary<string, IParseFieldOperation> operations = new Dictionary<string, IParseFieldOperation>
            {
                ["gogo"] = new Mock<IParseFieldOperation>().Object
            };

            Dictionary<string, object> responseDict = new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "_User",
                ["objectId"] = "d3ImSh3ki",
                ["sessionToken"] = "s3ss10nt0k3n",
                ["createdAt"] = "2015-09-18T18:11:28.943Z"
            };

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

            return new ParseUserController(mockRunner.Object, Client.Decoder).SignUpAsync(state, operations, Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "classes/_User"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
                Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserControllerTests))]
        public Task TestLogInWithUsernamePassword()
        {
            Dictionary<string, object> responseDict = new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "_User",
                ["objectId"] = "d3ImSh3ki",
                ["sessionToken"] = "s3ss10nt0k3n",
                ["createdAt"] = "2015-09-18T18:11:28.943Z"
            };

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

            return new ParseUserController(mockRunner.Object, Client.Decoder).LogInAsync("grantland", "123grantland123", Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "login?username=grantland&password=123grantland123"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
                Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserControllerTests))]
        public Task TestLogInWithAuthData()
        {
            Dictionary<string, object> responseDict = new Dictionary<string, object>
            {
                ["__type"] = "Object" ,
                ["className"] = "_User" ,
                ["objectId"] = "d3ImSh3ki" ,
                ["sessionToken"] = "s3ss10nt0k3n" ,
                ["createdAt"] = "2015-09-18T18:11:28.943Z" 
            };

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

            return new ParseUserController(mockRunner.Object, Client.Decoder).LogInAsync("facebook", data: null, serviceHub: Client, cancellationToken: CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "users"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
                Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserControllerTests))]
        public Task TestGetUserFromSessionToken()
        {
            Dictionary<string, object> responseDict = new Dictionary<string, object>
            {
                ["__type"] = "Object",
                ["className"] = "_User",
                ["objectId"] = "d3ImSh3ki",
                ["sessionToken"] = "s3ss10nt0k3n",
                ["createdAt"] = "2015-09-18T18:11:28.943Z"
            };

            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict));

            return new ParseUserController(mockRunner.Object, Client.Decoder).GetUserAsync("s3ss10nt0k3n", Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "users/me"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

                IObjectState newState = task.Result;
                Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
                Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
                Assert.IsNotNull(newState.CreatedAt);
                Assert.IsNotNull(newState.UpdatedAt);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(UserControllerTests))]
        public Task TestRequestPasswordReset()
        {
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { }));

            return new ParseUserController(mockRunner.Object, Client.Decoder).RequestPasswordResetAsync("gogo@parse.com", CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);

                mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<ParseCommand>(command => command.Path == "requestPasswordReset"), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            });
        }

        Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner> { };
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
