using Moq;
using Parse;
using Parse.Common.Internal;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class CommandTests
    {
        [TestInitialize]
        public void SetUp() => ParseClient.Initialize(new ParseClient.Configuration { ApplicationId = "", WindowsKey = "" });

        [TestCleanup]
        public void TearDown() => ParseCorePlugins.Instance.Reset();

        [TestMethod]
        public void TestMakeCommand()
        {
            ParseCommand command = new ParseCommand("endpoint", method: "GET", sessionToken: "abcd", headers: null, data: null);

            Assert.AreEqual("/1/endpoint", command.Uri.AbsolutePath);
            Assert.AreEqual("GET", command.Method);
            Assert.IsTrue(command.Headers.Any(pair => pair.Key == "X-Parse-Session-Token" && pair.Value == "abcd"));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommand()
        {
            var mockHttpClient = new Mock<IHttpClient>();
            var mockInstallationIdController = new Mock<IInstallationIdController>();
            var fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "{}"));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null)).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Result.Item2, typeof(IDictionary<string, object>));
                Assert.AreEqual(0, t.Result.Item2.Count);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithArrayResult()
        {
            var mockHttpClient = new Mock<IHttpClient>();
            var mockInstallationIdController = new Mock<IInstallationIdController>();
            var fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "[]"));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
                It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            ParseCommandRunner commandRunner = new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
            var command = new ParseCommand("endpoint", method: "GET", data: null);
            return commandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Result.Item2, typeof (IDictionary<string, object>));
                Assert.AreEqual(1, t.Result.Item2.Count);
                Assert.IsTrue(t.Result.Item2.ContainsKey("results"));
                Assert.IsInstanceOfType(t.Result.Item2["results"], typeof (IList<object>));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithInvalidString()
        {
            var mockHttpClient = new Mock<IHttpClient>();
            var mockInstallationIdController = new Mock<IInstallationIdController>();
            var fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "invalid"));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
                It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            ParseCommandRunner commandRunner = new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
            var command = new ParseCommand("endpoint", method: "GET", data: null);
            return commandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof (ParseException));
                Assert.AreEqual(ParseException.ErrorCode.OtherCause, (t.Exception.InnerException as ParseException).Code);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithErrorCode()
        {
            var mockHttpClient = new Mock<IHttpClient>();
            var mockInstallationIdController = new Mock<IInstallationIdController>();
            var fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, "{ \"code\": 101, \"error\": \"Object not found.\" }"));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
                It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            ParseCommandRunner commandRunner = new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
            var command = new ParseCommand("endpoint", method: "GET", data: null);
            return commandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof (ParseException));
                var parseException = t.Exception.InnerException as ParseException;
                Assert.AreEqual(ParseException.ErrorCode.ObjectNotFound, parseException.Code);
                Assert.AreEqual("Object not found.", parseException.Message);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithInternalServerError()
        {
            var mockHttpClient = new Mock<IHttpClient>();
            var mockInstallationIdController = new Mock<IInstallationIdController>();
            var fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, null));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
                It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            ParseCommandRunner commandRunner = new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
            var command = new ParseCommand("endpoint", method: "GET", data: null);
            return commandRunner.RunCommandAsync(command).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof (ParseException));
                Assert.AreEqual(ParseException.ErrorCode.InternalServerError, (t.Exception.InnerException as ParseException).Code);
            });
        }
    }
}
