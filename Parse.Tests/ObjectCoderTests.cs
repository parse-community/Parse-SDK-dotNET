using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;
using Parse.Infrastructure.Data;
using Parse.Platform.Objects;

namespace Parse.Tests
{
    [TestClass]
    public class ObjectCoderTests
    {
        [TestMethod]
        public void TestACLCoding()
        {
            MutableObjectState state = (MutableObjectState) ParseObjectCoder.Instance.Decode(new Dictionary<string, object>
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
            }, default, new ServiceHub { });

            ParseACL resultACL = default;

            Assert.IsTrue(state.ContainsKey("ACL"));
            Assert.IsTrue((resultACL = state.ServerData["ACL"] as ParseACL) is ParseACL);
            Assert.IsTrue(resultACL.PublicReadAccess);
            Assert.IsFalse(resultACL.PublicWriteAccess);
            Assert.IsTrue(resultACL.GetWriteAccess("3KmCvT7Zsb"));
            Assert.IsTrue(resultACL.GetReadAccess("3KmCvT7Zsb"));
            Assert.IsFalse(resultACL.GetWriteAccess("*"));
            Assert.IsTrue(resultACL.GetReadAccess("*"));
        }
    }
}
