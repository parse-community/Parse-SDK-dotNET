using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parse.Test
{
    [TestClass]
    public class ACLTests
    {
        [TestMethod]
        public void TestCheckPermissionsWithParseUserConstructor()
        {
            ParseUser owner = new ParseUser { Username = "TestOwnerUser" };
            ParseACL acl = new ParseACL(owner);
            Assert.IsTrue(acl.GetReadAccess(owner.ObjectId));
            Assert.IsTrue(acl.GetWriteAccess(owner.ObjectId));
            Assert.IsTrue(acl.GetReadAccess(owner));
            Assert.IsTrue(acl.GetWriteAccess(owner));
        }

        [TestMethod]
        public void TestReadWriteMutationWithParseUserConstructor()
        {
            ParseUser owner = new ParseUser { Username = "TestOwnerUser" };
            ParseUser otherUser = new ParseUser { Username = "OtherTestUser" };
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
        public void TestReadWriteMutationWithNullObjectIdParseUser()
        {
            ParseUser owner = new ParseUser { ObjectId = null };
            ParseACL acl = new ParseACL(owner);
            Assert.ThrowsException<ArgumentException>(() => acl.SetReadAccess(owner, false));
            Assert.ThrowsException<ArgumentException>(() => acl.SetWriteAccess(owner, false));
            Assert.ThrowsException<ArgumentException>(() => acl.SetReadAccess(owner.ObjectId, false));
            Assert.ThrowsException<ArgumentException>(() => acl.SetWriteAccess(owner.ObjectId, false));
        }
    }
}
