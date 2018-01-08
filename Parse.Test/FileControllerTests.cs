using Moq;
using Parse;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class FileControllerTests
    {
        [TestInitialize]
        public void SetUp() => ParseClient.Initialize(new ParseClient.Configuration { ApplicationId = "", WindowsKey = "" });

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSaveWithInvalidResult()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
            var mockRunner = CreateMockRunner(response);
            var state = new FileState
            {
                Name = "bekti.png",
                MimeType = "image/png"
            };

            var controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => Assert.IsTrue(t.IsFaulted));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSaveWithEmptyResult()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>());
            var mockRunner = CreateMockRunner(response);
            var state = new FileState
            {
                Name = "bekti.png",
                MimeType = "image/png"
            };

            var controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => Assert.IsTrue(t.IsFaulted));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSaveWithIncompleteResult()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
                new Dictionary<string, object>() {
            { "name", "newBekti.png"  },
                });
            var mockRunner = CreateMockRunner(response);
            var state = new FileState
            {
                Name = "bekti.png",
                MimeType = "image/png"
            };

            var controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => Assert.IsTrue(t.IsFaulted));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSave()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted,
                new Dictionary<string, object>() {
            { "name", "newBekti.png"  },
            { "url", "https://www.parse.com/newBekti.png" }
                });
            var mockRunner = CreateMockRunner(response);
            var state = new FileState
            {
                Name = "bekti.png",
                MimeType = "image/png"
            };

            var controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                var newState = t.Result;

                Assert.AreEqual(state.MimeType, newState.MimeType);
                Assert.AreEqual("newBekti.png", newState.Name);
                Assert.AreEqual("https://www.parse.com/newBekti.png", newState.Url.AbsoluteUri);
            });
        }

        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            var mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(),
                It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
                It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
