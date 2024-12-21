using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;

namespace Parse.Tests;

#warning Refactor if possible.

[TestClass]
public class ProgressTests
{
    [TestMethod]
    public void TestDownloadProgressEventGetterSetter()
    {
        IDataTransferLevel downloadProgressEvent = new DataTransferLevel { Amount = 0.5f };
        Assert.AreEqual(0.5f, downloadProgressEvent.Amount);

        downloadProgressEvent.Amount = 1.0f;
        Assert.AreEqual(1.0f, downloadProgressEvent.Amount);
    }

    [TestMethod]
    public void TestUploadProgressEventGetterSetter()
    {
        IDataTransferLevel uploadProgressEvent = new DataTransferLevel { Amount = 0.5f };
        Assert.AreEqual(0.5f, uploadProgressEvent.Amount);

        uploadProgressEvent.Amount = 1.0f;
        Assert.AreEqual(1.0f, uploadProgressEvent.Amount);
    }

    [TestMethod]
    public void TestObservingDownloadProgress()
    {
        int called = 0;
        Mock<IProgress<IDataTransferLevel>> mockProgress = new Mock<IProgress<IDataTransferLevel>>();
        mockProgress.Setup(obj => obj.Report(It.IsAny<IDataTransferLevel>())).Callback(() => called++);
        IProgress<IDataTransferLevel> progress = mockProgress.Object;

        progress.Report(new DataTransferLevel { Amount = 0.2f });
        progress.Report(new DataTransferLevel { Amount = 0.42f });
        progress.Report(new DataTransferLevel { Amount = 0.53f });
        progress.Report(new DataTransferLevel { Amount = 0.68f });
        progress.Report(new DataTransferLevel { Amount = 0.88f });

        Assert.AreEqual(5, called);
    }

    [TestMethod]
    public void TestObservingUploadProgress()
    {
        int called = 0;
        Mock<IProgress<IDataTransferLevel>> mockProgress = new Mock<IProgress<IDataTransferLevel>>();
        mockProgress.Setup(obj => obj.Report(It.IsAny<IDataTransferLevel>())).Callback(() => called++);
        IProgress<IDataTransferLevel> progress = mockProgress.Object;

        progress.Report(new DataTransferLevel { Amount = 0.2f });
        progress.Report(new DataTransferLevel { Amount = 0.42f });
        progress.Report(new DataTransferLevel { Amount = 0.53f });
        progress.Report(new DataTransferLevel { Amount = 0.68f });
        progress.Report(new DataTransferLevel { Amount = 0.88f });

        Assert.AreEqual(5, called);
    }
}
