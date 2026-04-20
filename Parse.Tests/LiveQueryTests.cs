using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class LiveQueryTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true }, new LiveQueryServerConnectionData { Test = true });

    public LiveQueryTests()
    {
        Client.Publicize();
    }

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
        Assert.IsInstanceOfType(buildParameters["where"], typeof(IDictionary<string, object>), "The 'where' parameter should be a Dictionary<string, object>.");
        Assert.IsInstanceOfType(buildParameters["keys"], typeof(string[]), "The 'keys' parameter should be a string array.");
        Assert.AreEqual("bar", ((IDictionary<string, object>) buildParameters["where"])["foo"], "The 'where' clause should match the query condition.");
        Assert.AreEqual("foo", ((string[]) buildParameters["keys"]).First(), "The 'keys' parameter should contain 'foo'.");
        Assert.IsFalse(buildParameters.ContainsKey("watch"), "The 'watch' parameter should not be present.");
        
    }

    [TestMethod]
    public void TestConstructorWithWatchedKeys()
    {
        ParseLiveQuery<ParseObject> liveQuery = new ParseLiveQuery<ParseObject>(
            Client.Services,
            "DummyClass",
            new Dictionary<string, object> { { "foo", "bar" } },
            ["foo"],
            ["foo"]);

        // Assert
        Assert.AreEqual("DummyClass", liveQuery.ClassName, "The ClassName property of liveQuery should be 'DummyClass'.");
        IDictionary<string, object> buildParameters = liveQuery.BuildParameters();
        Assert.AreEqual("DummyClass", buildParameters["className"], "The ClassName property of liveQuery should be 'DummyClass'.");
        Assert.IsTrue(buildParameters.ContainsKey("where"), "The 'where' key should be present in the build parameters.");
        Assert.IsTrue(buildParameters.ContainsKey("keys"), "The 'keys' key should be present in the build parameters.");
        Assert.IsInstanceOfType(buildParameters["where"], typeof(IDictionary<string, object>), "The 'where' parameter should be a Dictionary<string, object>.");
        Assert.IsInstanceOfType(buildParameters["keys"], typeof(string[]), "The 'keys' parameter should be a string array.");
        Assert.AreEqual("bar", ((IDictionary<string, object>) buildParameters["where"])["foo"], "The 'where' clause should match the query condition.");
        Assert.AreEqual("foo", ((string[]) buildParameters["keys"]).First(), "The 'keys' parameter should contain 'foo'.");
        Assert.IsInstanceOfType(buildParameters["watch"], typeof(string[]), "The 'watch' parameter should be a string array.");
        Assert.AreEqual("foo", ((string[]) buildParameters["watch"]).First(), "The 'watch' parameter should contain 'foo'.");
    }

    [TestMethod]
    public void TestGetLive()
    {
        // Arrange
        ParseQuery<ParseObject> query = Client.GetQuery("DummyClass")
            .WhereEqualTo("foo", "bar")
            .Select("foo");

        // Act
        ParseLiveQuery<ParseObject> liveQuery = query.GetLive();

        // Assert
        Assert.AreEqual("DummyClass", liveQuery.ClassName, "The ClassName property of liveQuery should be 'DummyClass'.");
        IDictionary<string, object> buildParameters = liveQuery.BuildParameters();
        Assert.AreEqual("DummyClass", buildParameters["className"], "The ClassName property of liveQuery should be 'DummyClass'.");
        Assert.IsTrue(buildParameters.ContainsKey("where"), "The 'where' key should be present in the build parameters.");
        Assert.IsTrue(buildParameters.ContainsKey("keys"), "The 'keys' key should be present in the build parameters.");
        Assert.IsInstanceOfType(buildParameters["where"], typeof(IDictionary<string, object>), "The 'where' parameter should be a Dictionary<string, object>.");
        Assert.IsInstanceOfType(buildParameters["keys"], typeof(string[]), "The 'keys' parameter should be a string array.");
        Assert.AreEqual("bar", ((Dictionary<string, object>) buildParameters["where"])["foo"], "The 'where' clause should match the query condition.");
        Assert.AreEqual("foo", ((string[]) buildParameters["keys"]).First(), "The 'keys' parameter should contain 'foo'.");
    }

    [TestMethod]
    public void TestWatch()
    {
        // Arrange
        ParseLiveQuery<ParseObject> liveQuery = new ParseLiveQuery<ParseObject>(
            Client.Services,
            "DummyClass",
            new Dictionary<string, object> { { "foo", "bar" } },
            ["foo"]);

        // Assert
        IDictionary<string, object> buildParameters = liveQuery.BuildParameters();
        Assert.IsFalse(buildParameters.ContainsKey("watch"), "The 'watch' key should not be present in the build parameters initially.");

        // Act
        liveQuery = liveQuery.Watch("foo");

        // Assert
        buildParameters = liveQuery.BuildParameters();
        Assert.IsInstanceOfType(buildParameters["watch"], typeof(string[]), "The 'watch' parameter should be a string array.");
        Assert.AreEqual("foo", ((string[]) buildParameters["watch"]).First(), "The 'watch' parameter should contain 'foo'.");

        // Act
        liveQuery = liveQuery.Watch("bar");

        // Assert
        buildParameters = liveQuery.BuildParameters();
        CollectionAssert.Contains((string[]) buildParameters["watch"], "bar", "The 'watch' parameter should contain 'bar'.");
    }
}
