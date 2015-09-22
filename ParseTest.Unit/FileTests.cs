using Moq;
using NUnit.Framework;
using Parse;
using Parse.Internal;
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
      ParseCorePlugins.Instance.FileController = null;
      ParseCorePlugins.Instance.CurrentUserController = null;
    }

    [Test]
    [AsyncStateMachine(typeof(FileTests))]
    public Task TestFileSave() {
      var response = new FileState {
        Name = "newBekti.png",
        Url = new Uri("https://www.parse.com/newBekti.png"),
        MimeType = "image/png"
      };
      var mockController = new Mock<IParseFileController>();
      mockController.Setup(obj => obj.SaveAsync(It.IsAny<FileState>(),
          It.IsAny<Stream>(),
          It.IsAny<string>(),
          It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
          It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();
      ParseCorePlugins.Instance.FileController = mockController.Object;
      ParseCorePlugins.Instance.CurrentUserController = mockCurrentUserController.Object;

      ParseFile file = new ParseFile("bekti.jpeg", new MemoryStream(), "image/jpeg");
      Assert.AreEqual("bekti.jpeg", file.Name);
      Assert.AreEqual("image/jpeg", file.MimeType);
      Assert.True(file.IsDirty);

      return file.SaveAsync().ContinueWith(t => {
        Assert.False(t.IsFaulted);
        Assert.AreEqual("newBekti.png", file.Name);
        Assert.AreEqual("image/png", file.MimeType);
        Assert.AreEqual("https://www.parse.com/newBekti.png", file.Url.AbsoluteUri);
        Assert.False(file.IsDirty);
      });
    }
  }
}
