using LeanCloud;
using LeanCloud.Core.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class UserControllerTests {
    [SetUp]
    public void SetUp() {
      AVClient.Initialize(new AVClient.Configuration {
        ApplicationId = "",
        ApplicationKey = ""
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserControllerTests))]
    public Task TestSignUp() {
      var state = new MutableObjectState {
        ClassName = "_User",
        ServerData = new Dictionary<string, object>() {
          { "username", "hallucinogen" },
          { "password", "secret" }
        }
      };
      var operations = new Dictionary<string, IAVFieldOperation>() {
        { "gogo", new Mock<IAVFieldOperation>().Object }
      };

      var responseDict = new Dictionary<string, object>() {
        { "__type", "Object" },
        { "className", "_User" },
        { "objectId", "d3ImSh3ki" },
        { "sessionToken", "s3ss10nt0k3n" },
        { "createdAt", "2015-09-18T18:11:28.943Z" }
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVUserController(mockRunner.Object);
      return controller.SignUpAsync(state, operations, CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);

        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/classes/_User"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));

        var newState = t.Result;
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.NotNull(newState.CreatedAt);
        Assert.NotNull(newState.UpdatedAt);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserControllerTests))]
    public Task TestLogInWithUsernamePassword() {
      var responseDict = new Dictionary<string, object>() {
        { "__type", "Object" },
        { "className", "_User" },
        { "objectId", "d3ImSh3ki" },
        { "sessionToken", "s3ss10nt0k3n" },
        { "createdAt", "2015-09-18T18:11:28.943Z" }
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVUserController(mockRunner.Object);
      return controller.LogInAsync("grantland", "123grantland123", CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);

        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/login"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));

        var newState = t.Result;
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.NotNull(newState.CreatedAt);
        Assert.NotNull(newState.UpdatedAt);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserControllerTests))]
    public Task TestLogInWithAuthData() {
      var responseDict = new Dictionary<string, object>() {
        { "__type", "Object" },
        { "className", "_User" },
        { "objectId", "d3ImSh3ki" },
        { "sessionToken", "s3ss10nt0k3n" },
        { "createdAt", "2015-09-18T18:11:28.943Z" }
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVUserController(mockRunner.Object);
      return controller.LogInAsync("facebook", data: null, cancellationToken: CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);

        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/users"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));

        var newState = t.Result;
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.NotNull(newState.CreatedAt);
        Assert.NotNull(newState.UpdatedAt);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserControllerTests))]
    public Task TestGetUserFromSessionToken() {
      var responseDict = new Dictionary<string, object>() {
        { "__type", "Object" },
        { "className", "_User" },
        { "objectId", "d3ImSh3ki" },
        { "sessionToken", "s3ss10nt0k3n" },
        { "createdAt", "2015-09-18T18:11:28.943Z" }
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVUserController(mockRunner.Object);
      return controller.GetUserAsync("s3ss10nt0k3n", CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);

        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/users/me"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));

        var newState = t.Result;
        Assert.AreEqual("s3ss10nt0k3n", newState["sessionToken"]);
        Assert.AreEqual("d3ImSh3ki", newState.ObjectId);
        Assert.NotNull(newState.CreatedAt);
        Assert.NotNull(newState.UpdatedAt);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(UserControllerTests))]
    public Task TestRequestPasswordReset() {
      var responseDict = new Dictionary<string, object>();
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVUserController(mockRunner.Object);
      return controller.RequestPasswordResetAsync("gogo@parse.com", CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);

        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/requestPasswordReset"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }

    private Mock<IAVCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response) {
      var mockRunner = new Mock<IAVCommandRunner>();
      mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<AVCommand>(),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()))
          .Returns(Task<Tuple<HttpStatusCode, IDictionary<string, object>>>.FromResult(response));

      return mockRunner;
    }
  }
}
