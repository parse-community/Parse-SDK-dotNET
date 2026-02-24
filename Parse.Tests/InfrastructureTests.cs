using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Utilities;

namespace Parse.Tests;

[TestClass]
public class InfrastructureTests
{
    [TestMethod]
    public void TestFlexibleDictionaryWrapper()
    {
        // Arrange
        Dictionary<string, int> inner = new()
        {
            { "char", 1 },
            { "string", 594 }
        };
        FlexibleDictionaryWrapper<int, int> wrapper = new(inner);

        // Act & Assert
        Assert.AreEqual<int>(1, wrapper["char"]);
        Assert.AreEqual<int>(594, wrapper["string"]);

        wrapper["new"] = 615;
        Assert.AreEqual<int>(615, inner["new"]);
    }

    [TestMethod]
    public void TestFlexibleListWrapper()
    {
        // Arrange
        List<object> inner = [1, 6.5f];
        FlexibleListWrapper<int, object> wrapper = new(inner);

        // Act & Assert
        Assert.AreEqual(1, wrapper[0]);
        Assert.AreEqual(6, wrapper[1]);

        wrapper.Add(2);
        Assert.AreEqual(2, inner[2]);
    }

    [TestMethod]
    public void TestConversion()
    {
        // Test basic conversions
        Assert.AreEqual(123, Conversion.ConvertTo<int>("123"));
        Assert.AreEqual(123L, Conversion.ConvertTo<long>(123));
        Assert.AreEqual(123.45, Conversion.ConvertTo<double>("123.45"));
    }

    [TestMethod]
    public void TestGetOrDefault()
    {
        Dictionary<string, string> dict = new Dictionary<string, string> { { "key", "value" } };
        Assert.AreEqual("value", dict.GetOrDefault("key", "default"));
        Assert.AreEqual("default", dict.GetOrDefault("missing", "default"));
    }

    [TestMethod]
    public void TestCollectionsEqual()
    {
        List<int> list1 = [1, 2, 3];
        List<int> list2 = [1, 2, 3];
        List<int> list3 = [1, 2, 4];

        Assert.IsTrue(list1.CollectionsEqual(list2));
        Assert.IsFalse(list1.CollectionsEqual(list3));
        Assert.IsFalse(list1.CollectionsEqual(null));
        Assert.IsTrue(((List<int>)null).CollectionsEqual(null));
    }
}
