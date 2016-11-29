using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class FileControllerTests {
    [SetUp]
    public void SetUp() {
      AVClient.Initialize(new AVClient.Configuration {
        ApplicationId = "",
        ApplicationKey = ""
      });
    }

    //[Test]
    //[AsyncStateMachine(typeof(FileControllerTests))]
    //public Task TestFileControllerSaveWithInvalidResult() {
    //  var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
    //  var mockRunner = CreateMockRunner(response);
    //  var state = new FileState {
    //    Name = "bekti.png",
    //    MimeType = "image/png"
    //  };

    //  var controller = new AVFileController(mockRunner.Object);
    //  return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => {
    //    Assert.True(t.IsFaulted);
    //  });
    //}

    //[Test]
    //[AsyncStateMachine(typeof(FileControllerTests))]
    //public Task TestFileControllerSaveWithEmptyResult() {
    //  var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>());
    //  var mockRunner = CreateMockRunner(response);
    //  var state = new FileState {
    //    Name = "bekti.png",
    //    MimeType = "image/png"
    //  };

    //  var controller = new AVFileController(mockRunner.Object);
    //  return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => {
    //    Assert.True(t.IsFaulted);
    //  });
    //}

    //[Test]
    //[AsyncStateMachine(typeof(FileControllerTests))]
    //public Task TestFileControllerSaveWithIncompleteResult() {
    //  var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
    //      new Dictionary<string, object>() {
    //        { "name", "newBekti.png"  },
    //      });
    //  var mockRunner = CreateMockRunner(response);
    //  var state = new FileState {
    //    Name = "bekti.png",
    //    MimeType = "image/png"
    //  };

    //  var controller = new AVFileController(mockRunner.Object);
    //  return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => {
    //    Assert.True(t.IsFaulted);
    //  });
    //}

    //[Test]
    //[AsyncStateMachine(typeof(FileControllerTests))]
    //public Task TestFileControllerSave() {
    //  var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
    //      new Dictionary<string, object>() {
    //        { "name", "newBekti.png"  },
    //        { "url", "https://www.parse.com/newBekti.png" }
    //      });
    //  var mockRunner = CreateMockRunner(response);
    //  var state = new FileState {
    //    Name = "bekti.png",
    //    MimeType = "image/png"
    //  };

    //  var controller = new AVFileController(mockRunner.Object);
    //  return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => {
    //    Assert.False(t.IsFaulted);
    //    var newState = t.Result;

    //    Assert.AreEqual(state.MimeType, newState.MimeType);
    //    Assert.AreEqual("newBekti.png", newState.Name);
    //    Assert.AreEqual("https://www.parse.com/newBekti.png", newState.Url.AbsoluteUri);
    //  });
    //}

    //private Mock<IAVCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response) {
    //  var mockRunner = new Mock<IAVCommandRunner>();
    //  mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<AVCommand>(),
    //      It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
    //      It.IsAny<IProgress<AVDownloadProgressEventArgs>>(),
    //      It.IsAny<CancellationToken>()))
    //      .Returns(Task<Tuple<HttpStatusCode, IDictionary<string, object>>>.FromResult(response));

    //  return mockRunner;
    //}
  }
}
