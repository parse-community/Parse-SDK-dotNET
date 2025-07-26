using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class LiveQueryTests
{
    public class DummyParseObject : ParseObject { }

    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize the client and ensure the instance is set
        Client = new ParseClient(new ServerConnectionData { Test = true }, new LiveQueryServerConnectionData { Test = true });
        Client.Publicize();
    }

    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestConstructor()
    {
        ParseLiveQuery<ParseObject> liveQuery = new ParseLiveQuery<ParseObject>(
            Client.Services,
            "DummyClass",
            new Dictionary<string, object> { { "foo", "bar" } },
            ["foo"]);

        // Assert
        Assert.AreEqual("DummyClass", liveQuery.ClassName, "The ClassName property of liveQuery should be 'DummyClass'.");
        IDictionary<string, object> buildParameters = liveQuery.BuildParameters();
        Assert.AreEqual("DummyClass", buildParameters["className"], "The ClassName property of liveQuery should be 'DummyClass'.");
        Assert.IsTrue(buildParameters.ContainsKey("where"), "The 'where' key should be present in the build parameters.");
        Assert.IsTrue(buildParameters.ContainsKey("keys"), "The 'keys' key should be present in the build parameters.");
        Assert.IsInstanceOfType<Dictionary<string, object>>(buildParameters["where"], "The 'where' parameter should be a Dictionary<string, object>.");
        Assert.IsInstanceOfType<string[]>(buildParameters["keys"], "The 'keys' parameter should be a string array.");
        Assert.AreEqual("bar", ((Dictionary<string, object>)buildParameters["where"])["foo"], "The 'where' clause should match the query condition.");
        Assert.AreEqual("foo", ((string[])buildParameters["keys"]).First(), "The 'keys' parameter should contain 'foo'.");
    }

    [TestMethod]
    public void TestGetLive()
    {
        // Arrange
        ParseQuery<ParseObject> query = Client.GetQuery("DummyClass")
            .WhereEqualTo("foo", "bar")
            .Select("foo");

        // Act
        ParseLiveQuery<ParseObject> liveQuery = query.GetLive()
            .Watch("foo");

        // Assert
        Assert.AreEqual("DummyClass", liveQuery.ClassName, "The ClassName property of liveQuery should be 'DummyClass'.");
        IDictionary<string, object> buildParameters = liveQuery.BuildParameters();
        Assert.AreEqual("DummyClass", buildParameters["className"], "The ClassName property of liveQuery should be 'DummyClass'.");
        Assert.IsTrue(buildParameters.ContainsKey("where"), "The 'where' key should be present in the build parameters.");
        Assert.IsTrue(buildParameters.ContainsKey("keys"), "The 'keys' key should be present in the build parameters.");
        Assert.IsTrue(buildParameters.ContainsKey("watch"), "The 'watch' key should be present in the build parameters.");
        Assert.IsInstanceOfType<Dictionary<string, object>>(buildParameters["where"], "The 'where' parameter should be a Dictionary<string, object>.");
        Assert.IsInstanceOfType<string[]>(buildParameters["keys"], "The 'keys' parameter should be a string array.");
        Assert.IsInstanceOfType<string[]>(buildParameters["watch"], "The 'watch' parameter should be a string array.");
        Assert.AreEqual("bar", ((Dictionary<string, object>)buildParameters["where"])["foo"], "The 'where' clause should match the query condition.");
        Assert.AreEqual("foo", ((string[])buildParameters["keys"]).First(), "The 'keys' parameter should contain 'foo'.");
        Assert.AreEqual("foo", ((string[])buildParameters["watch"]).First(), "The 'watch' parameter should contain 'foo'.");
    }
}
