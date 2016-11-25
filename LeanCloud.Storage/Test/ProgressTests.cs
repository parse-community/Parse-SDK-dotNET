using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class ProgressTests {
    [Test]
    public void TestDownloadProgressEventGetterSetter() {
      var downloadProgressEvent = new AVDownloadProgressEventArgs {
        Progress = 0.5f
      };
      Assert.AreEqual(0.5f, downloadProgressEvent.Progress);

      downloadProgressEvent.Progress = 1.0f;
      Assert.AreEqual(1.0f, downloadProgressEvent.Progress);
    }

    [Test]
    public void TestUploadProgressEventGetterSetter() {
      var uploadProgressEvent = new AVDownloadProgressEventArgs {
        Progress = 0.5f
      };
      Assert.AreEqual(0.5f, uploadProgressEvent.Progress);

      uploadProgressEvent.Progress = 1.0f;
      Assert.AreEqual(1.0f, uploadProgressEvent.Progress);
    }

    [Test]
    public void TestObservingDownloadProgress() {
      int called = 0;
      var mockProgress = new Mock<IProgress<AVDownloadProgressEventArgs>>();
      mockProgress.Setup(obj => obj.Report(It.IsAny<AVDownloadProgressEventArgs>())).Callback(() => {
        called++;
      });
      IProgress<AVDownloadProgressEventArgs> progress = mockProgress.Object;

      progress.Report(new AVDownloadProgressEventArgs { Progress = 0.2f });
      progress.Report(new AVDownloadProgressEventArgs { Progress = 0.42f });
      progress.Report(new AVDownloadProgressEventArgs { Progress = 0.53f });
      progress.Report(new AVDownloadProgressEventArgs { Progress = 0.68f });
      progress.Report(new AVDownloadProgressEventArgs { Progress = 0.88f });

      Assert.AreEqual(5, called);
    }

    [Test]
    public void TestObservingUploadProgress() {
      int called = 0;
      var mockProgress = new Mock<IProgress<AVUploadProgressEventArgs>>();
      mockProgress.Setup(obj => obj.Report(It.IsAny<AVUploadProgressEventArgs>())).Callback(() => {
        called++;
      });
      IProgress<AVUploadProgressEventArgs> progress = mockProgress.Object;

      progress.Report(new AVUploadProgressEventArgs { Progress = 0.2f });
      progress.Report(new AVUploadProgressEventArgs { Progress = 0.42f });
      progress.Report(new AVUploadProgressEventArgs { Progress = 0.53f });
      progress.Report(new AVUploadProgressEventArgs { Progress = 0.68f });
      progress.Report(new AVUploadProgressEventArgs { Progress = 0.88f });

      Assert.AreEqual(5, called);
    }
  }
}
