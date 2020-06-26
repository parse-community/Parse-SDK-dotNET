using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Internal;
using Parse.Abstractions.Platform.Files;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Platform.Files;

namespace Parse.Tests
{
    [TestClass]
    public class FileTests
    {
        [TestMethod]
        [AsyncStateMachine(typeof(FileTests))]
        public Task TestFileSave()
        {
            Mock<IParseFileController> mockController = new Mock<IParseFileController>();
            mockController.Setup(obj => obj.SaveAsync(It.IsAny<FileState>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new FileState { Name = "newBekti.png", Location = new Uri("https://www.parse.com/newBekti.png"), MediaType = "image/png" }));
            Mock<IParseCurrentUserController> mockCurrentUserController = new Mock<IParseCurrentUserController>();

            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, new MutableServiceHub { FileController = mockController.Object, CurrentUserController = mockCurrentUserController.Object });

            ParseFile file = new ParseFile("bekti.jpeg", new MemoryStream { }, "image/jpeg");

            Assert.AreEqual("bekti.jpeg", file.Name);
            Assert.AreEqual("image/jpeg", file.MimeType);
            Assert.IsTrue(file.IsDirty);

            return file.SaveAsync(client).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.AreEqual("newBekti.png", file.Name);
                Assert.AreEqual("image/png", file.MimeType);
                Assert.AreEqual("https://www.parse.com/newBekti.png", file.Url.AbsoluteUri);
                Assert.IsFalse(file.IsDirty);
            });
        }

        [TestMethod]
        public void TestSecureUrl()
        {
            Uri unsecureUri = new Uri("http://files.parsetfss.com/yolo.txt");
            Uri secureUri = new Uri("https://files.parsetfss.com/yolo.txt");
            Uri randomUri = new Uri("http://random.server.local/file.foo");

            ParseFile file = ParseFileExtensions.Create("Foo", unsecureUri);
            Assert.AreEqual(secureUri, file.Url);

            file = ParseFileExtensions.Create("Bar", secureUri);
            Assert.AreEqual(secureUri, file.Url);

            file = ParseFileExtensions.Create("Baz", randomUri);
            Assert.AreEqual(randomUri, file.Url);
        }
    }
}
