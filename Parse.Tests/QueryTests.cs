using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class QueryTests
{
    ParseClient Client { get; set; }
    Mock<IServiceHub> ServiceHubMock { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        ServiceHubMock = new Mock<IServiceHub>();
        Client = new ParseClient(new ServerConnectionData { Test = true })
        {
            Services = ServiceHubMock.Object
        };
        Client.Publicize();
    }

    [TestCleanup]
    public void Clean() => (Client.Services as ServiceHub)?.Reset();

    [TestMethod]
    public void TestQueryBuilder_WhereEqualTo()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .WhereEqualTo("foo", "bar");

        IDictionary<string, object> parameters = query.BuildParameters();
        IDictionary<string, object> where = parameters["where"] as IDictionary<string, object>;

        Assert.AreEqual("bar", where["foo"]);
    }

    [TestMethod]
    public void TestQueryBuilder_WhereNotEqualTo()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .WhereNotEqualTo("foo", "bar");

        IDictionary<string, object> parameters = query.BuildParameters();
        IDictionary<string, object> where = parameters["where"] as IDictionary<string, object>;
        IDictionary<string, object> condition = where["foo"] as IDictionary<string, object>;

        Assert.AreEqual("bar", condition["$ne"]);
    }

    [TestMethod]
    public void TestQueryBuilder_OrderBy()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .OrderBy("foo");

        IDictionary<string, object> parameters = query.BuildParameters();
        Assert.AreEqual("foo", parameters["order"]);
    }

    [TestMethod]
    public void TestQueryBuilder_OrderByDescending()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .OrderByDescending("foo");

        IDictionary<string, object> parameters = query.BuildParameters();
        Assert.AreEqual("-foo", parameters["order"]);
    }

    [TestMethod]
    public void TestQueryBuilder_SkipLimit()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .Skip(10)
            .Limit(20);

        IDictionary<string, object> parameters = query.BuildParameters();
        Assert.AreEqual(10, parameters["skip"]);
        Assert.AreEqual(20, parameters["limit"]);
    }

    [TestMethod]
    public void TestQueryBuilder_Include()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .Include("foo")
            .Include("bar");

        IDictionary<string, object> parameters = query.BuildParameters();
        Assert.AreEqual("foo,bar", parameters["include"]);
    }

    [TestMethod]
    public void TestQueryBuilder_Select()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .Select("foo")
            .Select("bar");

        IDictionary<string, object> parameters = query.BuildParameters();
        Assert.AreEqual("foo,bar", parameters["keys"]);
    }

    [TestMethod]
    public void TestQueryBuilder_WhereContainedIn()
    {
        List<string> values = ["a", "b", "c"];
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .WhereContainedIn("foo", values);

        IDictionary<string, object> parameters = query.BuildParameters();
        IDictionary<string, object> where = parameters["where"] as IDictionary<string, object>;
        IDictionary<string, object> condition = where["foo"] as IDictionary<string, object>;
        IEnumerable<object> list = condition["$in"] as IEnumerable<object>;

        CollectionAssert.AreEqual(values, list.ToList());
    }

    [TestMethod]
    public void TestQueryBuilder_WhereDoesNotExist()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .WhereDoesNotExist("foo");

        IDictionary<string, object> parameters = query.BuildParameters();
        IDictionary<string, object> where = parameters["where"] as IDictionary<string, object>;
        IDictionary<string, object> condition = where["foo"] as IDictionary<string, object>;

        Assert.IsFalse((bool)condition["$exists"]);
    }

    [TestMethod]
    public void TestQueryBuilder_WhereGreaterThan()
    {
        ParseQuery<ParseObject> query = new ParseQuery<ParseObject>(ServiceHubMock.Object, "TestClass")
            .WhereGreaterThan("foo", 10);

        IDictionary<string, object> parameters = query.BuildParameters();
        IDictionary<string, object> where = parameters["where"] as IDictionary<string, object>;
        IDictionary<string, object> condition = where["foo"] as IDictionary<string, object>;

        Assert.AreEqual(10, condition["$gt"]);
    }
}
