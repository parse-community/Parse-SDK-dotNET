using Moq;
using Parse;
using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class ProgressTests
    {
        [TestMethod]
        public void TestDownloadProgressEventGetterSetter()
        {
            var downloadProgressEvent = new ParseDownloadProgressEventArgs { Progress = 0.5f };
            Assert.AreEqual(0.5f, downloadProgressEvent.Progress);

            downloadProgressEvent.Progress = 1.0f;
            Assert.AreEqual(1.0f, downloadProgressEvent.Progress);
        }

        [TestMethod]
        public void TestUploadProgressEventGetterSetter()
        {
            var uploadProgressEvent = new ParseDownloadProgressEventArgs { Progress = 0.5f };
            Assert.AreEqual(0.5f, uploadProgressEvent.Progress);

            uploadProgressEvent.Progress = 1.0f;
            Assert.AreEqual(1.0f, uploadProgressEvent.Progress);
        }

        [TestMethod]
        public void TestObservingDownloadProgress()
        {
            int called = 0;
            var mockProgress = new Mock<IProgress<ParseDownloadProgressEventArgs>>();
            mockProgress.Setup(obj => obj.Report(It.IsAny<ParseDownloadProgressEventArgs>())).Callback(() => called++);
            IProgress<ParseDownloadProgressEventArgs> progress = mockProgress.Object;

            progress.Report(new ParseDownloadProgressEventArgs { Progress = 0.2f });
            progress.Report(new ParseDownloadProgressEventArgs { Progress = 0.42f });
            progress.Report(new ParseDownloadProgressEventArgs { Progress = 0.53f });
            progress.Report(new ParseDownloadProgressEventArgs { Progress = 0.68f });
            progress.Report(new ParseDownloadProgressEventArgs { Progress = 0.88f });

            Assert.AreEqual(5, called);
        }

        [TestMethod]
        public void TestObservingUploadProgress()
        {
            int called = 0;
            var mockProgress = new Mock<IProgress<ParseUploadProgressEventArgs>>();
            mockProgress.Setup(obj => obj.Report(It.IsAny<ParseUploadProgressEventArgs>())).Callback(() => called++);
            IProgress<ParseUploadProgressEventArgs> progress = mockProgress.Object;

            progress.Report(new ParseUploadProgressEventArgs { Progress = 0.2f });
            progress.Report(new ParseUploadProgressEventArgs { Progress = 0.42f });
            progress.Report(new ParseUploadProgressEventArgs { Progress = 0.53f });
            progress.Report(new ParseUploadProgressEventArgs { Progress = 0.68f });
            progress.Report(new ParseUploadProgressEventArgs { Progress = 0.88f });

            Assert.AreEqual(5, called);
        }
    }
}
