using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests
{
    [TestClass]
    public class ACLTests
    {
        ParseClient Client { get; set; } = new ParseClient(new ServerConnectionData { Test = true });

        [TestInitialize]
        public void Initialize()
        {
            Client.AddValidClass<ParseUser>();
            Client.AddValidClass<ParseSession>();
        }

        [TestCleanup]
        public void Clean() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestCheckPermissionsWithParseUserConstructor()
        {
            ParseUser owner = GenerateUser("OwnerUser");
            ParseUser user = GenerateUser("OtherUser");
            ParseACL acl = new ParseACL(owner);
            Assert.IsTrue(acl.GetReadAccess(owner.ObjectId));
            Assert.IsTrue(acl.GetWriteAccess(owner.ObjectId));
            Assert.IsTrue(acl.GetReadAccess(owner));
            Assert.IsTrue(acl.GetWriteAccess(owner));
        }

        [TestMethod]
        public void TestReadWriteMutationWithParseUserConstructor()
        {
            ParseUser owner = GenerateUser("OwnerUser");
            ParseUser otherUser = GenerateUser("OtherUser");
            ParseACL acl = new ParseACL(owner);
            acl.SetReadAccess(otherUser, true);
            acl.SetWriteAccess(otherUser, true);
            acl.SetReadAccess(owner.ObjectId, false);
            acl.SetWriteAccess(owner.ObjectId, false);
            Assert.IsTrue(acl.GetReadAccess(otherUser.ObjectId));
            Assert.IsTrue(acl.GetWriteAccess(otherUser.ObjectId));
            Assert.IsTrue(acl.GetReadAccess(otherUser));
            Assert.IsTrue(acl.GetWriteAccess(otherUser));
            Assert.IsFalse(acl.GetReadAccess(owner));
            Assert.IsFalse(acl.GetWriteAccess(owner));
        }

        [TestMethod]
        public void TestParseACLCreationWithNullObjectIdParseUser() => Assert.ThrowsException<ArgumentException>(() => new ParseACL(GenerateUser(default)));

        ParseUser GenerateUser(string objectID) => Client.GenerateObjectFromState<ParseUser>(new MutableObjectState { ObjectId = objectID }, "_User");
    }
}
