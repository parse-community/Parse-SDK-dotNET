using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Cloud;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class CloudTests
{
#warning Skipped post-test-evaluation cleaning method may be needed.

    // [TestCleanup]
    // public void TearDown() => ParseCorePlugins.Instance.Reset();

    [TestMethod]
    public async Task TestCloudFunctionsMissingResultAsync()
    {
        // Arrange
        var hub = new MutableServiceHub { };
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var mockController = new Mock<IParseCloudCodeController>();
        mockController
     .Setup(obj => obj.CallFunctionAsync<IDictionary<string, object>>(
         It.IsAny<string>(), // name
         It.IsAny<IDictionary<string, object>>(), // parameters
         It.IsAny<string>(), // sessionToken
         It.IsAny<IServiceHub>(), // serviceHub
         It.IsAny<CancellationToken>(), // cancellationToken
         It.IsAny<IProgress<IDataTransferLevel>>(), // uploadProgress
         It.IsAny<IProgress<IDataTransferLevel>>() // downloadProgress
     ))
     .ReturnsAsync(new Dictionary<string, object>
     {
         ["fosco"] = "ben",
         ["list"] = new List<object> { 1, 2, 3 }
     });


        hub.CloudCodeController = mockController.Object;
        hub.CurrentUserController = new Mock<IParseCurrentUserController>().Object;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ParseFailureException>(async () =>
            await client.CallCloudCodeFunctionAsync<IDictionary<string, object>>("someFunction", null, CancellationToken.None));
    }

}
