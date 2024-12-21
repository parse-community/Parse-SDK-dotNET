using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

[TestClass]
public class ObjectCoderTests
{
    [TestMethod]
    public void TestACLCoding()
    {
        // Prepare the mock service hub
        var serviceHub = new ServiceHub(); // Mock or actual implementation depending on your setup
        
        // Decode the ACL from a dictionary
        MutableObjectState state = (MutableObjectState) ParseObjectCoder.Instance.Decode(new Dictionary<string, object>
        {
            ["ACL"] = new Dictionary<string, object>
            {
                ["ACL"] = new Dictionary<string, object>
                {
                    ["3KmCvT7Zsb"] = new Dictionary<string, object>
                    {
                        ["read"] = true,
                        ["write"] = true
                    },
                    ["*"] = new Dictionary<string, object> { ["read"] = true }
                }
            }

        }, default, serviceHub);

        // Check that the ACL was properly decoded
        ParseACL resultACL = state.ServerData["ACL"] as ParseACL;
        Debug.WriteLine(resultACL is null);
        // Assertions
        Assert.IsTrue(state.ContainsKey("ACL"));
        Assert.IsNotNull(resultACL);
        Assert.IsTrue(resultACL.PublicReadAccess);
        Assert.IsFalse(resultACL.PublicWriteAccess);
        Assert.IsTrue(resultACL.GetWriteAccess("3KmCvT7Zsb"));
        Assert.IsTrue(resultACL.GetReadAccess("3KmCvT7Zsb"));
        Assert.IsFalse(resultACL.GetWriteAccess("*"));
        Assert.IsTrue(resultACL.GetReadAccess("*"));
    }

    public async Task FetchAsync_FetchesCorrectly() // Mock difficulty: 3
    {
        //Arrange
        var mockCommandRunner = new Mock<IParseCommandRunner>();
        var mockDecoder = new Mock<IParseDataDecoder>();
        var mockServiceHub = new Mock<IServiceHub>();
        var mockState = new Mock<IObjectState>();
        mockState.Setup(x => x.ClassName).Returns("TestClass");
        mockState.Setup(x => x.ObjectId).Returns("testId");

        mockDecoder.Setup(x => x.Decode(It.IsAny<IDictionary<string, object>>(), It.IsAny<IServiceHub>())).Returns(mockState.Object);
        mockCommandRunner.Setup(c => c.RunCommandAsync(It.IsAny<ParseCommand>(), null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new Tuple<HttpStatusCode, IDictionary<string, object>>(System.Net.HttpStatusCode.OK, new Dictionary<string, object>()));

        ParseObjectController controller = new ParseObjectController(mockCommandRunner.Object, mockDecoder.Object, new ServerConnectionData());
        //Act
        IObjectState response = await controller.FetchAsync(mockState.Object, "session", mockServiceHub.Object);

        //Assert
        mockCommandRunner.Verify(x => x.RunCommandAsync(It.IsAny<ParseCommand>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(response, mockState.Object);
    }
  
    [TestMethod]
    [Description("Tests DeleteAsync correctly deletes a ParseObject.")]
    public async Task DeleteAsync_DeletesCorrectly() // Mock difficulty: 3
    {
        //Arrange
        var mockCommandRunner = new Mock<IParseCommandRunner>();
        var mockDecoder = new Mock<IParseDataDecoder>();
        var mockServiceHub = new Mock<IServiceHub>();
        var mockState = new Mock<IObjectState>();
        mockState.Setup(x => x.ClassName).Returns("test");
        mockState.Setup(x => x.ObjectId).Returns("testId");

        mockCommandRunner.Setup(c => c.RunCommandAsync(It.IsAny<ParseCommand>(), null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new Tuple<HttpStatusCode, IDictionary<string, object>>(System.Net.HttpStatusCode.OK, new Dictionary<string, object>()));
        ParseObjectController controller = new ParseObjectController(mockCommandRunner.Object, mockDecoder.Object, new ServerConnectionData());

        //Act
        await controller.DeleteAsync(mockState.Object, "session");

        //Assert
        mockCommandRunner.Verify(x => x.RunCommandAsync(It.IsAny<ParseCommand>(), null, null, It.IsAny<CancellationToken>()), Times.Once);

    }
   
    [TestMethod]
    [Description("Tests that ExecuteBatchRequests correctly handles empty list.")]
    public void ExecuteBatchRequests_EmptyList()
    {
        var mockCommandRunner = new Mock<IParseCommandRunner>();
        var mockDecoder = new Mock<IParseDataDecoder>();
        var mockServiceHub = new Mock<IServiceHub>();
        ParseObjectController controller = new ParseObjectController(mockCommandRunner.Object, mockDecoder.Object, new ServerConnectionData());
        IList<ParseCommand> emptyList = new List<ParseCommand>();

        var task = controller.ExecuteBatchRequests(emptyList, "session", CancellationToken.None);

        Assert.AreEqual(0, task.Count);

    }
}
