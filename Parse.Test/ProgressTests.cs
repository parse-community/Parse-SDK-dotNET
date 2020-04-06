using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Parse.Test
{
    [TestClass]
    public class ProgressTests
    {
        [TestMethod]
        public void TestDownloadProgressEventGetterSetter()
        {
            DataRecievalPresenter downloadProgressEvent = new DataRecievalPresenter { Amount = 0.5f };
            Assert.AreEqual(0.5f, downloadProgressEvent.Amount);

            downloadProgressEvent.Amount = 1.0f;
            Assert.AreEqual(1.0f, downloadProgressEvent.Amount);
        }

        [TestMethod]
        public void TestUploadProgressEventGetterSetter()
        {
            DataRecievalPresenter uploadProgressEvent = new DataRecievalPresenter { Amount = 0.5f };
            Assert.AreEqual(0.5f, uploadProgressEvent.Amount);

            uploadProgressEvent.Amount = 1.0f;
            Assert.AreEqual(1.0f, uploadProgressEvent.Amount);
        }

        [TestMethod]
        public void TestObservingDownloadProgress()
        {
            int called = 0;
            Mock<IProgress<DataRecievalPresenter>> mockProgress = new Mock<IProgress<DataRecievalPresenter>>();
            mockProgress.Setup(obj => obj.Report(It.IsAny<DataRecievalPresenter>())).Callback(() => called++);
            IProgress<DataRecievalPresenter> progress = mockProgress.Object;

            progress.Report(new DataRecievalPresenter { Amount = 0.2f });
            progress.Report(new DataRecievalPresenter { Amount = 0.42f });
            progress.Report(new DataRecievalPresenter { Amount = 0.53f });
            progress.Report(new DataRecievalPresenter { Amount = 0.68f });
            progress.Report(new DataRecievalPresenter { Amount = 0.88f });

            Assert.AreEqual(5, called);
        }

        [TestMethod]
        public void TestObservingUploadProgress()
        {
            int called = 0;
            Mock<IProgress<DataTransmissionAdvancementLevel>> mockProgress = new Mock<IProgress<DataTransmissionAdvancementLevel>>();
            mockProgress.Setup(obj => obj.Report(It.IsAny<DataTransmissionAdvancementLevel>())).Callback(() => called++);
            IProgress<DataTransmissionAdvancementLevel> progress = mockProgress.Object;

            progress.Report(new DataTransmissionAdvancementLevel { Amount = 0.2f });
            progress.Report(new DataTransmissionAdvancementLevel { Amount = 0.42f });
            progress.Report(new DataTransmissionAdvancementLevel { Amount = 0.53f });
            progress.Report(new DataTransmissionAdvancementLevel { Amount = 0.68f });
            progress.Report(new DataTransmissionAdvancementLevel { Amount = 0.88f });

            Assert.AreEqual(5, called);
        }
    }
}
