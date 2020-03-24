using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Library;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Library;
using Parse.Management;

namespace Parse.Test
{
    [TestClass]
    public class CommandTests
    {
        Mock<IMetadataController> MockMetadataController { get; } = new Mock<IMetadataController> { };

        [TestInitialize]
        public void SetUp()
        {
            ParseClient.Initialize(new Configuration { ApplicationID = "", Key = "", Test = true });
            MockMetadataController.Setup(metadata => metadata.HostVersioningData).Returns(new HostApplicationVersioningData { BuildVersion = "1", DisplayVersion = "1", HostOSVersion = "1" });
        }

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
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationIdController = new Mock<IParseInstallationController>();
            Task<Tuple<HttpStatusCode, string>> fakeResponse = Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "{}"));
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(fakeResponse);

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object, MockMetadataController.Object).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null)).ContinueWith(t =>
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
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationIdController = new Mock<IParseInstallationController>();
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "[]")));

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object, MockMetadataController.Object).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null)).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Result.Item2, typeof(IDictionary<string, object>));
                Assert.AreEqual(1, t.Result.Item2.Count);
                Assert.IsTrue(t.Result.Item2.ContainsKey("results"));
                Assert.IsInstanceOfType(t.Result.Item2["results"], typeof(IList<object>));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithInvalidString()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationIdController = new Mock<IParseInstallationController>();
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "invalid")));

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object, MockMetadataController.Object).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null)).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof(ParseException));
                Assert.AreEqual(ParseException.ErrorCode.OtherCause, (t.Exception.InnerException as ParseException).Code);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithErrorCode()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationIdController = new Mock<IParseInstallationController>();
            mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, "{ \"code\": 101, \"error\": \"Object not found.\" }")));

            mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object, MockMetadataController.Object).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null)).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof(ParseException));
                ParseException parseException = t.Exception.InnerException as ParseException;
                Assert.AreEqual(ParseException.ErrorCode.ObjectNotFound, parseException.Code);
                Assert.AreEqual("Object not found.", parseException.Message);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CommandTests))]
        public Task TestRunCommandWithInternalServerError()
        {
            Mock<IWebClient> mockHttpClient = new Mock<IWebClient>();
            Mock<IParseInstallationController> mockInstallationIdController = new Mock<IParseInstallationController>();
            
            mockHttpClient.Setup(client => client.ExecuteAsync(It.IsAny<HttpRequest>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, null)));
            mockInstallationIdController.Setup(installationController => installationController.GetAsync()).Returns(Task.FromResult<Guid?>(null));

            return new ParseCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object, MockMetadataController.Object).RunCommandAsync(new ParseCommand("endpoint", method: "GET", data: null)).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof(ParseException));
                Assert.AreEqual(ParseException.ErrorCode.InternalServerError, (t.Exception.InnerException as ParseException).Code);
            });
        }
    }
}
