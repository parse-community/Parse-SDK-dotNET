using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Common.Internal;
using LeanCloud.Core.Internal;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class CommandTests {
    [SetUp]
    public void SetUp() {
      AVClient.Initialize(new AVClient.Configuration {
        ApplicationId = "",
        WindowsKey = ""
      });
    }

    [TearDown]
    public void TearDown() {
      AVPlugins.Instance.Reset();
    }

    [Test]
    public void TestMakeCommand() {
      AVCommand command = new AVCommand("endpoint",
          method: "GET",
          sessionToken: "abcd",
          headers: null,
          data: null);

      Assert.AreEqual("/1/endpoint", command.Uri.AbsolutePath);
      Assert.AreEqual("GET", command.Method);
      Assert.IsTrue(command.Headers.Any(pair => pair.Key == "X-LeanCloud-Session-Token" && pair.Value == "abcd"));
    }

    [Test]
    [AsyncStateMachine(typeof(CommandTests))]
    public Task TestRunCommand() {
      var mockHttpClient = new Mock<IHttpClient>();
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var fakeResponse = Task<Tuple<HttpStatusCode, string>>.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "{}"));
      mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>())).Returns(fakeResponse);

      mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

      AVCommandRunner commandRunner = new AVCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
      var command = new AVCommand("endpoint", method: "GET", data: null);
      return commandRunner.RunCommandAsync(command).ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        Assert.IsInstanceOf(typeof(IDictionary<string, object>), t.Result.Item2);
        Assert.AreEqual(0, t.Result.Item2.Count);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CommandTests))]
    public Task TestRunCommandWithArrayResult() {
      var mockHttpClient = new Mock<IHttpClient>();
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var fakeResponse = Task<Tuple<HttpStatusCode, string>>.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "[]"));
      mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>())).Returns(fakeResponse);

      mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

      AVCommandRunner commandRunner = new AVCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
      var command = new AVCommand("endpoint", method: "GET", data: null);
      return commandRunner.RunCommandAsync(command).ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        Assert.IsInstanceOf<IDictionary<string, object>>(t.Result.Item2);
        Assert.AreEqual(1, t.Result.Item2.Count);
        Assert.True(t.Result.Item2.ContainsKey("results"));
        Assert.IsInstanceOf<IList<object>>(t.Result.Item2["results"]);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CommandTests))]
    public Task TestRunCommandWithInvalidString() {
      var mockHttpClient = new Mock<IHttpClient>();
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var fakeResponse = Task<Tuple<HttpStatusCode, string>>.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.OK, "invalid"));
      mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>())).Returns(fakeResponse);

      mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

      AVCommandRunner commandRunner = new AVCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
      var command = new AVCommand("endpoint", method: "GET", data: null);
      return commandRunner.RunCommandAsync(command).ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.False(t.IsCanceled);
        Assert.IsInstanceOf<AVException>(t.Exception.InnerException);
        var parseException = t.Exception.InnerException as AVException;
        Assert.AreEqual(AVException.ErrorCode.OtherCause, parseException.Code);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CommandTests))]
    public Task TestRunCommandWithErrorCode() {
      var mockHttpClient = new Mock<IHttpClient>();
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var fakeResponse = Task<Tuple<HttpStatusCode, string>>.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, "{ \"code\": 101, \"error\": \"Object not found.\" }"));
      mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>())).Returns(fakeResponse);

      mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

      AVCommandRunner commandRunner = new AVCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
      var command = new AVCommand("endpoint", method: "GET", data: null);
      return commandRunner.RunCommandAsync(command).ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.False(t.IsCanceled);
        Assert.IsInstanceOf<AVException>(t.Exception.InnerException);
        var parseException = t.Exception.InnerException as AVException;
        Assert.AreEqual(AVException.ErrorCode.ObjectNotFound, parseException.Code);
        Assert.AreEqual("Object not found.", parseException.Message);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CommandTests))]
    public Task TestRunCommandWithInternalServerError() {
      var mockHttpClient = new Mock<IHttpClient>();
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var fakeResponse = Task<Tuple<HttpStatusCode, string>>.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, null));
      mockHttpClient.Setup(obj => obj.ExecuteAsync(It.IsAny<HttpRequest>(),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>())).Returns(fakeResponse);

      mockInstallationIdController.Setup(i => i.GetAsync()).Returns(Task.FromResult<Guid?>(null));

      AVCommandRunner commandRunner = new AVCommandRunner(mockHttpClient.Object, mockInstallationIdController.Object);
      var command = new AVCommand("endpoint", method: "GET", data: null);
      return commandRunner.RunCommandAsync(command).ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.False(t.IsCanceled);
        Assert.IsInstanceOf<AVException>(t.Exception.InnerException);
        var parseException = t.Exception.InnerException as AVException;
        Assert.AreEqual(AVException.ErrorCode.InternalServerError, parseException.Code);
      });
    }
  }
}
