using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure.Execution;

namespace Parse.Tests
{
#warning Initialization and cleaning steps may be redundant for each test method. It may be possible to simply reset the required services before each run.
#warning Class refactoring requires completion.

    [TestClass]
    public class CommandTests
    {
        ParseClient Client { get; set; }

        [TestInitialize]
        public void Initialize() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

        [TestCleanup]
        public void Clean() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestMakeCommand()
        {
            ParseCommand command = new ParseCommand("endpoint", method: "GET", sessionToken: "abcd", headers: default, data: default);

            Assert.AreEqual("endpoint", command.Path);
            Assert.AreEqual("GET", command.Method);
            Assert.IsTrue(command.Headers.Any(pair => pair.Key == "X-Parse-Session-Token" && pair.Value == "abcd"));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommand()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationController = new Mock<IParseInstallationController>();
            Task<Tuple<HttpStatusCode, string>> fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "{}"));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<Infrastructure.Execution.WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationController.Setup(installation => installation.GetAsync()).Returns(Task.FromResult<Guid?>(default));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationController.Object, Client.MetadataController, Client.ServerConnectionData, new Lazy<IParseUserController>(() => Client.UserController)).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: default)).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsInstanceOfType(task.Result.Item2, typeof(IDictionary<string, object>));
                Assert.AreEqual(0, task.Result.Item2.Count);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithArrayResult()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationController = new Mock<IParseInstallationController>();
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<Infrastructure.Execution.WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "[]")));

            mockInstallationController.Setup(installation => installation.GetAsync()).Returns(Task.FromResult<Guid?>(default));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationController.Object, Client.MetadataController, Client.ServerConnectionData, new Lazy<IParseUserController>(() => Client.UserController)).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: default)).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsInstanceOfType(task.Result.Item2, typeof(IDictionary<string, object>));
                Assert.AreEqual(1, task.Result.Item2.Count);
                Assert.IsTrue(task.Result.Item2.ContainsKey("results"));
                Assert.IsInstanceOfType(task.Result.Item2["results"], typeof(IList<object>));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithInvalidString()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationController = new Mock<IParseInstallationController>();
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<Infrastructure.Execution.WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "invalid")));

            mockInstallationController.Setup(controller => controller.GetAsync()).Returns(Task.FromResult<Guid?>(default));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationController.Object, Client.MetadataController, Client.ServerConnectionData, new Lazy<IParseUserController>(() => Client.UserController)).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: default)).ContinueWith(task =>
            {
                Assert.IsTrue(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsInstanceOfType(task.Exception.InnerException, typeof(ParseFailureException));
                Assert.AreEqual(ParseFailureException.ErrorCode.OtherCause, (task.Exception.InnerException as ParseFailureException).Code);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithErrorCode()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationController = new Mock<IParseInstallationController>();
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<Infrastructure.Execution.WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, "{ \"code\": 101, \"error\": \"Object not found.\" }")));

            mockInstallationController.Setup(controller => controller.GetAsync()).Returns(Task.FromResult<Guid?>(default));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationController.Object, Client.MetadataController, Client.ServerConnectionData, new Lazy<IParseUserController>(() => Client.UserController)).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: default)).ContinueWith(task =>
            {
                Assert.IsTrue(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsInstanceOfType(task.Exception.InnerException, typeof(ParseFailureException));
                ParseFailureException parseException = task.Exception.InnerException as ParseFailureException;
                Assert.AreEqual(ParseFailureException.ErrorCode.ObjectNotFound, parseException.Code);
                Assert.AreEqual("Object not found.", parseException.Message);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithInternalServerError()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationController = new Mock<IParseInstallationController>();
            
            mockHttpClient.Setup(client => client.ExecuteAsync(It.IsAny<Infrastructure.Execution.WebRequest>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, default)));
            mockInstallationController.Setup(installationController => installationController.GetAsync()).Returns(Task.FromResult<Guid?>(default));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationController.Object, Client.MetadataController, Client.ServerConnectionData, new Lazy<IParseUserController>(() => Client.UserController)).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: default)).ContinueWith(task =>
            {
                Assert.IsTrue(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsInstanceOfType(task.Exception.InnerException, typeof(ParseFailureException));
                Assert.AreEqual(ParseFailureException.ErrorCode.InternalServerError, (task.Exception.InnerException as ParseFailureException).Code);
            });
        }
    }
}
