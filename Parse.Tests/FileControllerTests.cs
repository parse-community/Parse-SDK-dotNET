using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure.Execution;
using Parse.Platform.Files;

namespace Parse.Tests
{
#warning Refactor this class.
#warning Skipped initialization step may be needed.

    [TestClass]
    public class FileControllerTests
    {
        // public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSaveWithInvalidResult()
        {
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);
            FileState state = new FileState
            {
                Name = "bekti.png",
                MediaType = "image/png"
            };

            ParseFileController controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => Assert.IsTrue(t.IsFaulted));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSaveWithEmptyResult()
        {
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>());
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);
            FileState state = new FileState
            {
                Name = "bekti.png",
                MediaType = "image/png"
            };

            ParseFileController controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => Assert.IsTrue(t.IsFaulted));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSaveWithIncompleteResult()
        {
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { ["name"] = "newBekti.png" });
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);
            FileState state = new FileState
            {
                Name = "bekti.png",
                MediaType = "image/png"
            };

            ParseFileController controller = new ParseFileController(mockRunner.Object);
            return controller.SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t => Assert.IsTrue(t.IsFaulted));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(FileControllerTests))]
        public Task TestFileControllerSave()
        {
            FileState state = new FileState
            {
                Name = "bekti.png",
                MediaType = "image/png"
            };

            return new ParseFileController(CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { ["name"] = "newBekti.png", ["url"] = "https://www.parse.com/newBekti.png" })).Object).SaveAsync(state, dataStream: new MemoryStream(), sessionToken: null, progress: null).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                FileState newState = t.Result;

                Assert.AreEqual(state.MediaType, newState.MediaType);
                Assert.AreEqual("newBekti.png", newState.Name);
                Assert.AreEqual("https://www.parse.com/newBekti.png", newState.Location.AbsoluteUri);
            });
        }

        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner>();
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
