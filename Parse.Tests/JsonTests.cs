using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Utilities;

namespace Parse.Tests;

[TestClass]
public class JsonTests
{
    [TestMethod]
    public void TestEmptyJsonStringFail()
    {
        var result = JsonUtilities.Parse("");
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(Dictionary<string, object>));
        Assert.AreEqual(0, ((Dictionary<string, object>) result).Count);
    }

    [TestMethod] //updated
    public void TestInvalidJsonStringAsRootFail()
    {
        // Expect empty dictionary for whitespace inputs
        Assert.IsInstanceOfType(JsonUtilities.Parse("\n"), typeof(Dictionary<string, object>));
        Assert.IsInstanceOfType(JsonUtilities.Parse("\t"), typeof(Dictionary<string, object>));
        Assert.IsInstanceOfType(JsonUtilities.Parse("   "), typeof(Dictionary<string, object>));

        // Expect exceptions for invalid JSON strings
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("a"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("abc"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("\u1234"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("1234"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("1,3"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{1"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("3}"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("}"));
    }


    [TestMethod]
    public void TestEmptyJsonObject() => Assert.IsTrue(JsonUtilities.Parse("{}") is IDictionary);

    [TestMethod]
    public void TestEmptyJsonArray() => Assert.IsTrue(JsonUtilities.Parse("[]") is IList);

    [TestMethod]
    public void TestOneJsonObject()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{ 1 }"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{ 1 : 1 }"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{ 1 : \"abc\" }"));

        object parsed = JsonUtilities.Parse("{\"abc\" : \"def\"}");
        Assert.IsTrue(parsed is IDictionary);
        IDictionary parsedDict = parsed as IDictionary;
        Assert.AreEqual("def", parsedDict["abc"]);

        parsed = JsonUtilities.Parse("{\"abc\" : {} }");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.IsTrue(parsedDict["abc"] is IDictionary);

        parsed = JsonUtilities.Parse("{\"abc\" : \"6060\"}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.AreEqual("6060", parsedDict["abc"]);

        parsed = JsonUtilities.Parse("{\"\" : \"\"}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.AreEqual("", parsedDict[""]);

        parsed = JsonUtilities.Parse("{\" abc\" : \"def \"}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.AreEqual("def ", parsedDict[" abc"]);

        parsed = JsonUtilities.Parse("{\"1\" : 6060}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.AreEqual((long) 6060, parsedDict["1"]);

        parsed = JsonUtilities.Parse("{\"1\" : null}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.IsNull(parsedDict["1"]);

        parsed = JsonUtilities.Parse("{\"1\" : true}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.IsTrue((bool) parsedDict["1"]);

        parsed = JsonUtilities.Parse("{\"1\" : false}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.IsFalse((bool) parsedDict["1"]);
    }

    [TestMethod]
    public void TestMultipleJsonObjectAsRootFail()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{},"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{\"abc\" : \"def\"},"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{\"abc\" : \"def\" \"def\"}"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{}, {}"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{},\n{}"));
    }

    [TestMethod]
    public void TestOneJsonArray()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[ 1 : 1 ]"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[ 1 1 ]"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[ 1 : \"1\" ]"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[ \"1\" : \"1\" ]"));

        object parsed = JsonUtilities.Parse("[ 1 ]");
        Assert.IsTrue(parsed is IList);
        IList parsedList = parsed as IList;
        Assert.AreEqual((long) 1, parsedList[0]);

        parsed = JsonUtilities.Parse("[ \n ]");
        Assert.IsTrue(parsed is IList);
        parsedList = parsed as IList;
        Assert.AreEqual(0, parsedList.Count);

        parsed = JsonUtilities.Parse("[ \"asdf\" ]");
        Assert.IsTrue(parsed is IList);
        parsedList = parsed as IList;
        Assert.AreEqual("asdf", parsedList[0]);

        parsed = JsonUtilities.Parse("[ \"\u849c\" ]");
        Assert.IsTrue(parsed is IList);
        parsedList = parsed as IList;
        Assert.AreEqual("\u849c", parsedList[0]);
    }

    [TestMethod]
    public void TestMultipleJsonArrayAsRootFail()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[],"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[\"abc\" : \"def\"],"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[], []"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[],\n[]"));
    }

    [TestMethod]
    public void TestJsonArrayInsideJsonObject()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{ [] }"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{ [], [] }"));
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("{ \"abc\": [], [] }"));

        object parsed = JsonUtilities.Parse("{ \"abc\": [] }");
        Assert.IsTrue(parsed is IDictionary);
        IDictionary parsedDict = parsed as IDictionary;
        Assert.IsTrue(parsedDict["abc"] is IList);

        parsed = JsonUtilities.Parse("{ \"6060\" :\n[ 6060 ]\t}");
        Assert.IsTrue(parsed is IDictionary);
        parsedDict = parsed as IDictionary;
        Assert.IsTrue(parsedDict["6060"] is IList);
        IList parsedList = parsedDict["6060"] as IList;
        Assert.AreEqual((long) 6060, parsedList[0]);
    }

    [TestMethod]
    public void TestJsonObjectInsideJsonArray()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("[ {} : {} ]"));

        // whitespace test
        object parsed = JsonUtilities.Parse("[\t\n{}\r\t]");
        Assert.IsTrue(parsed is IList);
        IList parsedList = parsed as IList;
        Assert.IsTrue(parsedList[0] is IDictionary);

        parsed = JsonUtilities.Parse("[ {}, { \"final\" : \"fantasy\"} ]");
        Assert.IsTrue(parsed is IList);
        parsedList = parsed as IList;
        Assert.IsTrue(parsedList[0] is IDictionary);
        Assert.IsTrue(parsedList[1] is IDictionary);
        IDictionary parsedDictionary = parsedList[1] as IDictionary;
        Assert.AreEqual("fantasy", parsedDictionary["final"]);
    }

    [TestMethod]
    public void TestJsonObjectWithElements()
    {
        // Just make sure they don't throw exception as we already check their content correctness
        // in other unit tests.
        JsonUtilities.Parse("{ \"mura\": \"masa\" }");
        JsonUtilities.Parse("{ \"mura\": 1234 }");
        JsonUtilities.Parse("{ \"mura\": { \"masa\": 1234 } }");
        JsonUtilities.Parse("{ \"mura\": { \"masa\": [ 1234 ] } }");
        JsonUtilities.Parse("{ \"mura\": { \"masa\": [ 1234 ] }, \"arr\": [] }");
    }

    [TestMethod]
    public void TestJsonArrayWithElements()
    {
        // Just make sure they don't throw exception as we already check their content correctness
        // in other unit tests.
        JsonUtilities.Parse("[ \"mura\" ]");
        JsonUtilities.Parse("[ \"\u1234\" ]");
        JsonUtilities.Parse("[ \"\u1234ff\", \"\u1234\" ]");
        JsonUtilities.Parse("[ [], [], [], [] ]");
        JsonUtilities.Parse("[ [], [ {}, {} ], [ {} ], [] ]");
    }

    [TestMethod]
    public void TestEncodeJson()
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        string encoded = JsonUtilities.Encode(dict);
        Assert.AreEqual("{}", encoded);

        List<object> list = new List<object>();
        encoded = JsonUtilities.Encode(list);
        Assert.AreEqual("[]", encoded);

        Dictionary<string, object> dictChild = new Dictionary<string, object>();
        list.Add(dictChild);
        encoded = JsonUtilities.Encode(list);
        Assert.AreEqual("[{}]", encoded);

        list.Add("1234          a\t\r\n");
        list.Add(1234);
        list.Add(12.34);
        list.Add(1.23456789123456789);
        encoded = JsonUtilities.Encode(list);

        // This string should be [{},\"1234          a\\t\\r\\n\",1234,12.34,1.23456789123457] for .NET Framework (https://github.com/dotnet/runtime/issues/31483).

        Assert.AreEqual("[{},\"1234          a\\t\\r\\n\",1234,12.34,1.234567891234568]", encoded);

        dict["arr"] = new List<object>();
        encoded = JsonUtilities.Encode(dict);
        Assert.AreEqual("{\"arr\":[]}", encoded);

        dict["\u1234"] = "\u1234";
        encoded = JsonUtilities.Encode(dict);
        Assert.AreEqual("{\"arr\":[],\"\u1234\":\"\u1234\"}", encoded);

        encoded = JsonUtilities.Encode(new List<object> { true, false, null });
        Assert.AreEqual("[true,false,null]", encoded);
    }

    [TestMethod]
    public void TestSpecialJsonNumbersAndModifiers()
    {
        Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("+123456789"));

        JsonUtilities.Parse("{ \"mura\": -123456789123456789 }");
        JsonUtilities.Parse("{ \"mura\": 1.1234567891234567E308 }");
        JsonUtilities.Parse("{ \"PI\": 3.141e-10 }");
        JsonUtilities.Parse("{ \"PI\": 3.141E-10 }");

        Assert.AreEqual(123456789123456789, (JsonUtilities.Parse("{ \"mura\": 123456789123456789 }") as IDictionary)["mura"]);
    }


    [TestMethod]
    public void TestJsonNumbersAndValueRanges()
    {
        //Assert.ThrowsException<ArgumentException>(() => JsonUtilities.Parse("+123456789"));
        Assert.IsInstanceOfType((JsonUtilities.Parse("{ \"long\": " + long.MaxValue + " }") as IDictionary)["long"], typeof(long));
        Assert.IsInstanceOfType((JsonUtilities.Parse("{ \"long\": " + long.MinValue + " }") as IDictionary)["long"], typeof(long));

        Assert.AreEqual((JsonUtilities.Parse("{ \"long\": " + long.MaxValue + " }") as IDictionary)["long"], long.MaxValue);
        Assert.AreEqual((JsonUtilities.Parse("{ \"long\": " + long.MinValue + " }") as IDictionary)["long"], long.MinValue);


        Assert.IsInstanceOfType((JsonUtilities.Parse("{ \"double\": " + double.MaxValue.ToString(CultureInfo.InvariantCulture) + " }") as IDictionary)["double"], typeof(double));
        Assert.IsInstanceOfType((JsonUtilities.Parse("{ \"double\": " + double.MinValue.ToString(CultureInfo.InvariantCulture) + " }") as IDictionary)["double"], typeof(double));

        Assert.AreEqual((JsonUtilities.Parse("{ \"double\": " + double.MaxValue.ToString(CultureInfo.InvariantCulture) + " }") as IDictionary)["double"], double.MaxValue);
        Assert.AreEqual((JsonUtilities.Parse("{ \"double\": " + double.MinValue.ToString(CultureInfo.InvariantCulture) + " }") as IDictionary)["double"], double.MinValue);

        double outOfInt64RangeValue = -9223372036854776000d;
        Assert.IsInstanceOfType((JsonUtilities.Parse("{ \"double\": " + outOfInt64RangeValue.ToString(CultureInfo.InvariantCulture) + " }") as IDictionary)["double"], typeof(double));
        Assert.AreEqual((JsonUtilities.Parse("{ \"double\": " + outOfInt64RangeValue.ToString(CultureInfo.InvariantCulture) + " }") as IDictionary)["double"], outOfInt64RangeValue);
    }

}
