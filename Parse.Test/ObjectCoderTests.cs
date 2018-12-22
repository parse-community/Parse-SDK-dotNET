using System.Xml.XPath;
using System.Linq;
using System.Diagnostics;
using Castle.DynamicProxy.Generators.Emitters;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using Parse;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
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
            }, null);

            ParseACL resultACL = null;
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
