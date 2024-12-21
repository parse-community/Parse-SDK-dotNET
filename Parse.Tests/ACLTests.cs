using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.Objects;
using Parse;
using System.Collections.Generic;
using System;

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
        Client.AddValidClass<ParseRole>();
    }

    [TestCleanup]
    public void Clean() => (Client.Services as ServiceHub)?.Reset();

    [TestMethod]
    [Description("Tests if default ParseACL is created without errors.")]
    public void TestParseACLDefaultConstructor() // Mock difficulty: 1
    {
        var acl = new ParseACL();
        Assert.IsNotNull(acl);

    }
    [TestMethod]
    [Description("Tests ACL creation using ParseUser constructor.")]
    public void TestCheckPermissionsWithParseUserConstructor() // Mock difficulty: 1
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
    [Description("Tests that users permission change accordingly")]
    public void TestReadWriteMutationWithParseUserConstructor()// Mock difficulty: 1
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
    [Description("Tests if throws if try to instantiate using a ParseUser without objectId.")]
    public void TestParseACLCreationWithNullObjectIdParseUser() // Mock difficulty: 1
    {
        // Assert
        Assert.ThrowsException<ArgumentException>(() => new ParseACL(GenerateUser(default)));
    }

    ParseUser GenerateUser(string objectID)
    {
        // Use the mock to simulate generating a ParseUser
        var state = new MutableObjectState { ObjectId = objectID, ClassName = "_User" };
        return Client.GenerateObjectFromState<ParseUser>(state, "_User");

    }

    [TestMethod]
    [Description("Tests to create a ParseUser via IParseClassController, that is set when calling Bind.")]
    public void TestGenerateObjectFromState() // Mock difficulty: 1
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
    [TestMethod]
    [Description("Tests for public read and write access values.")]
    public void TestPublicReadWriteAccessValues() // Mock difficulty: 1
    {
        var acl = new ParseACL();
        Assert.IsFalse(acl.PublicReadAccess);
        Assert.IsFalse(acl.PublicWriteAccess);

        acl.PublicReadAccess = true;
        acl.PublicWriteAccess = true;
        Assert.IsTrue(acl.PublicReadAccess);
        Assert.IsTrue(acl.PublicWriteAccess);
    }

    [TestMethod]
    [Description("Tests that sets and gets properly for string UserIds.")]
    public void TestSetGetAccessWithStringId() // Mock difficulty: 1
    {
        var acl = new ParseACL();
        var testUser = GenerateUser("test");
        acl.SetReadAccess(testUser.ObjectId, true);
        acl.SetWriteAccess(testUser.ObjectId, true);

        Assert.IsTrue(acl.GetReadAccess(testUser.ObjectId));
        Assert.IsTrue(acl.GetWriteAccess(testUser.ObjectId));

        acl.SetReadAccess(testUser.ObjectId, false);
        acl.SetWriteAccess(testUser.ObjectId, false);

        Assert.IsFalse(acl.GetReadAccess(testUser.ObjectId));
        Assert.IsFalse(acl.GetWriteAccess(testUser.ObjectId));
    }

    [TestMethod]
    [Description("Tests that methods thow exceptions if user id is null.")]
    public void SetGetAccessThrowsForNull() // Mock difficulty: 1
    {
        var acl = new ParseACL();

        Assert.ThrowsException<ArgumentException>(() => acl.SetReadAccess(userId:null, false));
        Assert.ThrowsException<ArgumentException>(() => acl.SetWriteAccess(userId: null, false));
        Assert.ThrowsException<ArgumentException>(() => acl.GetReadAccess(userId:null));
        Assert.ThrowsException<ArgumentException>(() => acl.GetWriteAccess(userId:null));

    }
    [TestMethod]
    [Description("Tests that a Get access using a ParseUser is correct.")]
    public void TestSetGetAccessWithParseUser() // Mock difficulty: 1
    {
        var acl = new ParseACL();
        ParseUser test = GenerateUser("test");

        acl.SetReadAccess(test, true);
        acl.SetWriteAccess(test, true);
        Assert.IsTrue(acl.GetReadAccess(test));
        Assert.IsTrue(acl.GetWriteAccess(test));

        acl.SetReadAccess(test, false);
        acl.SetWriteAccess(test, false);

        Assert.IsFalse(acl.GetReadAccess(test));
        Assert.IsFalse(acl.GetWriteAccess(test));

    }

    [TestMethod]
    [Description("Tests that the default ParseACL returns correct roles for read/write")]
    public void TestDefaultRolesForReadAndWriteAccess() // Mock difficulty: 1
    {
        var acl = new ParseACL();
        Assert.IsFalse(acl.GetRoleReadAccess("role"));
        Assert.IsFalse(acl.GetRoleWriteAccess("role"));

    }

    [TestMethod]
    [Description("Tests role read/write access with role names correctly and get methods.")]
    public void TestSetGetRoleReadWriteAccessWithRoleName() // Mock difficulty: 1
    {
        var acl = new ParseACL();
        acl.SetRoleReadAccess("test", true);
        acl.SetRoleWriteAccess("test", true);
        Assert.IsTrue(acl.GetRoleReadAccess("test"));
        Assert.IsTrue(acl.GetRoleWriteAccess("test"));

        acl.SetRoleReadAccess("test", false);
        acl.SetRoleWriteAccess("test", false);
        Assert.IsFalse(acl.GetRoleReadAccess("test"));
        Assert.IsFalse(acl.GetRoleWriteAccess("test"));
    }

    [TestMethod]
    [Description("Tests ACL can use and correctly convert to JSON object via ConvertToJSON.")]
    public void TestConvertToJSON() // Mock difficulty: 3
    {
        var acl = new ParseACL();
        ParseUser user = GenerateUser("test");

        acl.SetReadAccess(user, true);
        acl.SetWriteAccess(user, false);
        acl.SetRoleReadAccess("test", true);
        var json = (acl as IJsonConvertible).ConvertToJSON();
        Assert.IsInstanceOfType(json, typeof(IDictionary<string, object>));

        var jsonObject = json as IDictionary<string, object>;
        Assert.IsTrue(jsonObject.ContainsKey(user.ObjectId));
        Assert.IsTrue(jsonObject.ContainsKey("role:test"));
        var test = jsonObject[user.ObjectId] as Dictionary<string, object>;
        Assert.AreEqual(1, test.Count);
    }


    [TestMethod]
    [Description("Tests that ProcessAclData can handle invalid values for public key.")]
    public void TestProcessAclData_HandlesInvalidDataForPublic() // Mock difficulty: 1
    {
        var aclData = new Dictionary<string, object> { { "*", 123 } };
        var acl = new ParseACL(aclData);
        Assert.IsFalse(acl.PublicReadAccess);
        Assert.IsFalse(acl.PublicWriteAccess);
    }
    [TestMethod]
    [Description("Tests if ACL skips keys that don't represent valid JSON data dictionaries")]
    public void TestProcessAclData_SkipsInvalidKeys()  // Mock difficulty: 1
    {
        var aclData = new Dictionary<string, object> {
              {"userId", 123 }
              };
        var acl = new ParseACL(aclData);

        Assert.IsFalse(acl.GetReadAccess("userId"));
        Assert.IsFalse(acl.GetWriteAccess("userId"));
    }
}