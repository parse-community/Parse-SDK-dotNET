using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;

namespace Parse.Tests;


[TestClass]
public class ProgressTests
{
    private Mock<IProgress<IDataTransferLevel>> mockProgress;
    private int _callbackCallCount;


    [TestInitialize]
    public void Initialize()
    {
        mockProgress = new Mock<IProgress<IDataTransferLevel>>();
        _callbackCallCount = 0;
        mockProgress.Setup(obj => obj.Report(It.IsAny<IDataTransferLevel>()))
                    .Callback(() => _callbackCallCount++);

    }

    [TestCleanup]
    public void Cleanup()
    {
        mockProgress = null; // Ensure mock is released
    }

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
        IProgress<IDataTransferLevel> progress = mockProgress.Object;

        progress.Report(new DataTransferLevel { Amount = 0.2f });
        progress.Report(new DataTransferLevel { Amount = 0.42f });
        progress.Report(new DataTransferLevel { Amount = 0.53f });
        progress.Report(new DataTransferLevel { Amount = 0.68f });
        progress.Report(new DataTransferLevel { Amount = 0.88f });

        Assert.AreEqual(5, _callbackCallCount);
    }

    [TestMethod]
    public void TestObservingUploadProgress()
    {
        IProgress<IDataTransferLevel> progress = mockProgress.Object;

        progress.Report(new DataTransferLevel { Amount = 0.2f });
        progress.Report(new DataTransferLevel { Amount = 0.42f });
        progress.Report(new DataTransferLevel { Amount = 0.53f });
        progress.Report(new DataTransferLevel { Amount = 0.68f });
        progress.Report(new DataTransferLevel { Amount = 0.88f });

        Assert.AreEqual(5, _callbackCallCount);
    }
}