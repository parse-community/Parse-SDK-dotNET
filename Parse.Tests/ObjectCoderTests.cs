using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse;
using Parse.Infrastructure;
using Parse.Infrastructure.Data;
using Parse.Platform.Objects;
using System.Collections.Generic;
using System.Diagnostics;

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
}
