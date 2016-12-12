using LeanCloud;
using LeanCloud.Core.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class SessionControllerTests {
    [SetUp]
    public void SetUp() {
      AVClient.Initialize(new AVClient.Configuration {
        ApplicationId = "",
        ApplicationKey = ""
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionControllerTests))]
    public Task TestGetSessionWithEmptyResult() {
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVSessionController(mockRunner.Object);
      return controller.GetSessionAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.False(t.IsCanceled);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionControllerTests))]
    public Task TestGetSession() {
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
          new Dictionary<string, object>() {
            { "__type", "Object" },
            { "className", "Session" },
            { "sessionToken", "S0m3Se551on" },
            { "restricted", true }
          });
      var mockRunner = CreateMockRunner(response);

      var controller = new AVSessionController(mockRunner.Object);
      return controller.GetSessionAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.False(t.IsCanceled);
        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/sessions/me"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));

        var session = t.Result;
        Assert.AreEqual(2, session.Count());
        Assert.True((bool)session["restricted"]);
        Assert.AreEqual("S0m3Se551on", session["sessionToken"]);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionControllerTests))]
    public Task TestRevoke() {
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
      var mockRunner = CreateMockRunner(response);

      var controller = new AVSessionController(mockRunner.Object);
      return controller.RevokeAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/logout"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));
      });
    }

    [Test]
    [AsyncStateMachine(typeof(SessionControllerTests))]
    public Task TestUpgradeToRevocableSession() {
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
          new Dictionary<string, object>() {
            { "__type", "Object" },
            { "className", "Session" },
            { "sessionToken", "S0m3Se551on" },
            { "restricted", true }
          });
      var mockRunner = CreateMockRunner(response);

      var controller = new AVSessionController(mockRunner.Object);
      return controller.UpgradeToRevocableSessionAsync("S0m3Se551on", CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        mockRunner.Verify(obj => obj.RunCommandAsync(It.Is<AVCommand>(command => command.Uri.AbsolutePath == "/1/upgradeToRevocableSession"),
          It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
          It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()), Times.Exactly(1));

        var session = t.Result;
        Assert.AreEqual(2, session.Count());
        Assert.True((bool)session["restricted"]);
        Assert.AreEqual("S0m3Se551on", session["sessionToken"]);
      });
    }

    [Test]
    public void TestIsRevocableSessionToken() {
      IAVSessionController sessionController = new AVSessionController(Mock.Of<IAVCommandRunner>());
      Assert.True(sessionController.IsRevocableSessionToken("r:session"));
      Assert.True(sessionController.IsRevocableSessionToken("r:session:r:"));
      Assert.True(sessionController.IsRevocableSessionToken("session:r:"));
      Assert.False(sessionController.IsRevocableSessionToken("session:s:d:r"));
      Assert.False(sessionController.IsRevocableSessionToken("s:ession:s:d:r"));
      Assert.False(sessionController.IsRevocableSessionToken(""));
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
