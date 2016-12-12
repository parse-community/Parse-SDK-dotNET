using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class FileTests {
    [TearDown]
    public void TearDown() {
      AVPlugins.Instance = null;
    }

    //[Test]
    //[AsyncStateMachine(typeof(FileTests))]
    //public Task TestFileSave() {
    //  var response = new FileState {
    //    Name = "newBekti.png",
    //    Url = new Uri("https://www.parse.com/newBekti.png"),
    //    MimeType = "image/png"
    //  };
    //  var mockController = new Mock<IAVFileController>();
    //  mockController.Setup(obj => obj.SaveAsync(It.IsAny<FileState>(),
    //      It.IsAny<Stream>(),
    //      It.IsAny<string>(),
    //      It.IsAny<IProgress<AVUploadProgressEventArgs>>(),
    //      It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));
    //  var mockCurrentUserController = new Mock<IAVCurrentUserController>();
    //  AVPlugins.Instance = new AVPlugins {
    //    FileController = mockController.Object,
    //    CurrentUserController = mockCurrentUserController.Object
    //  };

    //  AVFile file = new AVFile("bekti.jpeg", new MemoryStream(), "image/jpeg");
    //  Assert.AreEqual("bekti.jpeg", file.Name);
    //  Assert.AreEqual("image/jpeg", file.MimeType);
    //  Assert.True(file.IsDirty);

    //  return file.SaveAsync().ContinueWith(t => {
    //    Assert.False(t.IsFaulted);
    //    Assert.AreEqual("newBekti.png", file.Name);
    //    Assert.AreEqual("image/png", file.MimeType);
    //    Assert.AreEqual("https://www.parse.com/newBekti.png", file.Url.AbsoluteUri);
    //    Assert.False(file.IsDirty);
    //  });
    //}

    //[Test]
    //public void TestSecureUrl() {
    //  Uri unsecureUri = new Uri("http://files.parsetfss.com/yolo.txt");
    //  Uri secureUri = new Uri("https://files.parsetfss.com/yolo.txt");
    //  Uri randomUri = new Uri("http://random.server.local/file.foo");

    //  AVFile file = AVFileExtensions.Create("Foo", unsecureUri);
    //  Assert.AreEqual(secureUri, file.Url);

    //  file = AVFileExtensions.Create("Bar", secureUri);
    //  Assert.AreEqual(secureUri, file.Url);

    //  file = AVFileExtensions.Create("Baz", randomUri);
    //  Assert.AreEqual(randomUri, file.Url);
    //}
  }
}
