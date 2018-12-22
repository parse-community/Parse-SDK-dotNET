using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Test
{
    [TestClass]
    public class ACLTests
    {
        [TestInitialize]
        public void SetUp()
        {
            ParseObject.RegisterSubclass<ParseUser>();
            ParseObject.RegisterSubclass<ParseSession>();
        }

        [TestCleanup]
        public void TearDown() => ParseCorePlugins.Instance = null;

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
        public void TestParseACLCreationWithNullObjectIdParseUser() => Assert.ThrowsException<ArgumentException>(() => new ParseACL(GenerateUser(null)));

        ParseUser GenerateUser(string objectID) => ParseObjectExtensions.FromState<ParseUser>(new MutableObjectState { ObjectId = objectID }, "_User");
    }
}
