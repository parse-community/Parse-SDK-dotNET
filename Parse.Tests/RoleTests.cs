using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class RoleTests
{
    ParseClient Client { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();
    }

    [TestCleanup]
    public void Clean() => (Client.Services as ServiceHub)?.Reset();

    [TestMethod]
    public void TestRoleConstructor()
    {
        ParseACL acl = new();
        ParseRole role = new("Administrators", acl);
        Assert.AreEqual("Administrators", role.Name);
        Assert.AreEqual(acl, role.ACL);
    }

    [TestMethod]
    [DataRow("ValidName")]
    [DataRow("Valid-Name")]
    [DataRow("Valid_Name")]
    [DataRow("Valid Name")]
    [DataRow("123")]
    public void TestRoleNameValidation_Valid(string name)
    {
        ParseACL acl = new();
        ParseRole role = new()
        {
            ACL = acl,

            // Valid names
            Name = name
        };
        Assert.AreEqual(name, role.Name);
    }

    [TestMethod]
    [DataRow("Invalid@Name")]
    [DataRow("Invalid#Name")]
    [DataRow("Invalid$Name")]
    [DataRow("Invalid!Name")]
    [DataRow("Invalid/Name")]
    [DataRow("Invalid\\Name")]
    public void TestRoleNameValidation_Invalid(string name)
    {
        ParseACL acl = new();
        ParseRole role = new() { ACL = acl};

        Assert.ThrowsExactly<ArgumentException>(() => role.Name = name);
    }

    [TestMethod]
    public void TestRoleNameImmutableAfterSave()
    {
        MutableObjectState state = new()
        { ObjectId = "existingId" };
        ParseRole role = Client.GenerateObjectFromState<ParseRole>(state, "_Role");
        
        Assert.ThrowsExactly<InvalidOperationException>(() => role.Name = "NewName");
    }
}
