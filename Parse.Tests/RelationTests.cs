using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Internal;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class RelationTests
{
    [TestMethod]
    public void TestRelationQuery()
    {
        ParseObject parent = new ServiceHub { }.CreateObjectWithoutData("Foo", "abcxyz");

        ParseRelation<ParseObject> relation = parent.GetRelation<ParseObject>("child");
        ParseQuery<ParseObject> query = relation.Query;

        // Client side, the query will appear to be for the wrong class.
        // When the server recieves it, the class name will be redirected using the 'redirectClassNameForKey' option.
        Assert.AreEqual("Foo", query.GetClassName());

        IDictionary<string, object> encoded = query.BuildParameters();

        Assert.AreEqual("child", encoded["redirectClassNameForKey"]);
    }
}