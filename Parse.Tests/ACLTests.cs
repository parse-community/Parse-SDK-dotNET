using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq; // Add Moq for mocking if not already added
using Parse.Infrastructure;
using Parse.Platform.Objects;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class ACLTests
{
    ParseClient Client { get; set; }

    Mock<IServiceHub> ServiceHubMock { get; set; }
    Mock<IParseObjectClassController> ClassControllerMock { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        // Mock ServiceHub
        ServiceHubMock = new Mock<IServiceHub>();
        ClassControllerMock = new Mock<IParseObjectClassController>();

        // Mock ClassController behavior
        ServiceHubMock.Setup(hub => hub.ClassController).Returns(ClassControllerMock.Object);

        // Mock ClassController.Instantiate behavior
        ClassControllerMock.Setup(controller => controller.Instantiate(It.IsAny<string>(), It.IsAny<IServiceHub>()))
            .Returns<string, IServiceHub>((className, hub) =>
            {
                var user = new ParseUser();
                user.Bind(hub); // Ensure the object is bound to the service hub
                return user;
            });

        // Set up ParseClient with the mocked ServiceHub
        Client = new ParseClient(new ServerConnectionData { Test = true })
        {
            Services = ServiceHubMock.Object
        };

        // Publicize the client to set ParseClient.Instance
        Client.Publicize();

        // Add valid classes to the client
        Client.AddValidClass<ParseUser>();
        Client.AddValidClass<ParseSession>();
    }

    [TestCleanup]
    public void Clean() => (Client.Services as ServiceHub)?.Reset();

    [TestMethod]
    public void TestCheckPermissionsWithParseUserConstructor()
    {
        // Arrange
        ParseUser owner = GenerateUser("OwnerUser");
        ParseUser user = GenerateUser("OtherUser");

        // Act
        ParseACL acl = new ParseACL(owner);

        // Assert
        Assert.IsTrue(acl.GetReadAccess(owner.ObjectId));
        Assert.IsTrue(acl.GetWriteAccess(owner.ObjectId));
        Assert.IsTrue(acl.GetReadAccess(owner));
        Assert.IsTrue(acl.GetWriteAccess(owner));
    }

    [TestMethod]
    public void TestReadWriteMutationWithParseUserConstructor()
    {
        // Arrange
        ParseUser owner = GenerateUser("OwnerUser");
        ParseUser otherUser = GenerateUser("OtherUser");

        // Act
        ParseACL acl = new ParseACL(owner);
        acl.SetReadAccess(otherUser, true);
        acl.SetWriteAccess(otherUser, true);
        acl.SetReadAccess(owner.ObjectId, false);
        acl.SetWriteAccess(owner.ObjectId, false);

        // Assert
        Assert.IsTrue(acl.GetReadAccess(otherUser.ObjectId));
        Assert.IsTrue(acl.GetWriteAccess(otherUser.ObjectId));
        Assert.IsTrue(acl.GetReadAccess(otherUser));
        Assert.IsTrue(acl.GetWriteAccess(otherUser));
        Assert.IsFalse(acl.GetReadAccess(owner));
        Assert.IsFalse(acl.GetWriteAccess(owner));
    }

    [TestMethod]
    public void TestParseACLCreationWithNullObjectIdParseUser()
    {
        // Assert
        Assert.ThrowsException<ArgumentException>(() => new ParseACL(GenerateUser(default)));
    }

    ParseUser GenerateUser(string objectID)
    {
        // Use the mock to simulate generating a ParseUser
        var state = new MutableObjectState { ObjectId = objectID };
        return Client.GenerateObjectFromState<ParseUser>(state, "_User");
    }

    [TestMethod]
    public void TestGenerateObjectFromState()
    {
        // Arrange
        var state = new MutableObjectState { ObjectId = "123", ClassName = null };
        var defaultClassName = "_User";

        var serviceHubMock = new Mock<IServiceHub>();
        var classControllerMock = new Mock<IParseObjectClassController>();

        classControllerMock.Setup(controller => controller.Instantiate(It.IsAny<string>(), It.IsAny<IServiceHub>()))
            .Returns<string, IServiceHub>((className, hub) => new ParseUser());

        // Act
        var user = classControllerMock.Object.GenerateObjectFromState<ParseUser>(state, defaultClassName, serviceHubMock.Object);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(defaultClassName, user.ClassName);
    }

}
