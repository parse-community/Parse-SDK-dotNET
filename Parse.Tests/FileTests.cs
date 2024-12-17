using System;
using System.IO;
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

namespace Parse.Tests;

[TestClass]
public class FileTests
{
    [TestMethod]
    public async Task TestFileSaveAsync()
    {
        // Arrange: Set up mock controllers and client
        var mockController = new Mock<IParseFileController>();
        mockController
            .Setup(obj => obj.SaveAsync(
                It.IsAny<FileState>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<IDataTransferLevel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileState
            {
                Name = "newBekti.png",
                Location = new Uri("https://www.parse.com/newBekti.png"),
                MediaType = "image/png"
            });

        var mockCurrentUserController = new Mock<IParseCurrentUserController>();

        var client = new ParseClient(
            new ServerConnectionData { Test = true },
            new MutableServiceHub
            {
                FileController = mockController.Object,
                CurrentUserController = mockCurrentUserController.Object
            });

        var file = new ParseFile("bekti.jpeg", new MemoryStream(), "image/jpeg");

        // Act: Save the file using the Parse client
        Assert.AreEqual("bekti.jpeg", file.Name);
        Assert.AreEqual("image/jpeg", file.MimeType);
        Assert.IsTrue(file.IsDirty);

        await file.SaveAsync(client);

        // Assert: Verify file properties and state after saving
        Assert.AreEqual("newBekti.png", file.Name);
        Assert.AreEqual("image/png", file.MimeType);
        Assert.AreEqual("https://www.parse.com/newBekti.png", file.Url.AbsoluteUri);
        Assert.IsFalse(file.IsDirty);

        // Verify the SaveAsync method was called on the mock controller
        mockController.Verify(obj => obj.SaveAsync(
            It.IsAny<FileState>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<IProgress<IDataTransferLevel>>(),
            It.IsAny<CancellationToken>()), Times.Once);
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
