using Parse.Common.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class JsonTests
    {
        [TestMethod]
        public void TestEmptyJsonStringFail() => Assert.ThrowsException<ArgumentException>(() => Json.Parse(""));

        [TestMethod]
        public void TestInvalidJsonStringAsRootFail()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("\n"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("a"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("abc"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("\u1234"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("\t"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("\t\n\r"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("   "));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("1234"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("1,3"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{1"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("3}"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("}"));
        }

        [TestMethod]
        public void TestEmptyJsonObject() => Assert.IsTrue(Json.Parse("{}") is IDictionary);

        [TestMethod]
        public void TestEmptyJsonArray() => Assert.IsTrue(Json.Parse("[]") is IList);

        [TestMethod]
        public void TestOneJsonObject()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{ 1 }"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{ 1 : 1 }"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{ 1 : \"abc\" }"));

            var parsed = Json.Parse("{\"abc\" : \"def\"}");
            Assert.IsTrue(parsed is IDictionary);
            var parsedDict = parsed as IDictionary;
            Assert.AreEqual("def", parsedDict["abc"]);

            parsed = Json.Parse("{\"abc\" : {} }");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.IsTrue(parsedDict["abc"] is IDictionary);

            parsed = Json.Parse("{\"abc\" : \"6060\"}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.AreEqual("6060", parsedDict["abc"]);

            parsed = Json.Parse("{\"\" : \"\"}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.AreEqual("", parsedDict[""]);

            parsed = Json.Parse("{\" abc\" : \"def \"}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.AreEqual("def ", parsedDict[" abc"]);

            parsed = Json.Parse("{\"1\" : 6060}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.AreEqual((Int64)6060, parsedDict["1"]);

            parsed = Json.Parse("{\"1\" : null}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.IsNull(parsedDict["1"]);

            parsed = Json.Parse("{\"1\" : true}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.IsTrue((bool)parsedDict["1"]);

            parsed = Json.Parse("{\"1\" : false}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.IsFalse((bool)parsedDict["1"]);
        }

        [TestMethod]
        public void TestMultipleJsonObjectAsRootFail()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{},"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{\"abc\" : \"def\"},"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{\"abc\" : \"def\" \"def\"}"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{}, {}"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{},\n{}"));
        }

        [TestMethod]
        public void TestOneJsonArray()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[ 1 : 1 ]"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[ 1 1 ]"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[ 1 : \"1\" ]"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[ \"1\" : \"1\" ]"));

            var parsed = Json.Parse("[ 1 ]");
            Assert.IsTrue(parsed is IList);
            var parsedList = parsed as IList;
            Assert.AreEqual((Int64)1, parsedList[0]);

            parsed = Json.Parse("[ \n ]");
            Assert.IsTrue(parsed is IList);
            parsedList = parsed as IList;
            Assert.AreEqual(0, parsedList.Count);

            parsed = Json.Parse("[ \"asdf\" ]");
            Assert.IsTrue(parsed is IList);
            parsedList = parsed as IList;
            Assert.AreEqual("asdf", parsedList[0]);

            parsed = Json.Parse("[ \"\u849c\" ]");
            Assert.IsTrue(parsed is IList);
            parsedList = parsed as IList;
            Assert.AreEqual("\u849c", parsedList[0]);
        }

        [TestMethod]
        public void TestMultipleJsonArrayAsRootFail()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[],"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[\"abc\" : \"def\"],"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[], []"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[],\n[]"));
        }

        [TestMethod]
        public void TestJsonArrayInsideJsonObject()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{ [] }"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{ [], [] }"));
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("{ \"abc\": [], [] }"));

            var parsed = Json.Parse("{ \"abc\": [] }");
            Assert.IsTrue(parsed is IDictionary);
            var parsedDict = parsed as IDictionary;
            Assert.IsTrue(parsedDict["abc"] is IList);

            parsed = Json.Parse("{ \"6060\" :\n[ 6060 ]\t}");
            Assert.IsTrue(parsed is IDictionary);
            parsedDict = parsed as IDictionary;
            Assert.IsTrue(parsedDict["6060"] is IList);
            var parsedList = parsedDict["6060"] as IList;
            Assert.AreEqual((Int64)6060, parsedList[0]);
        }

        [TestMethod]
        public void TestJsonObjectInsideJsonArray()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("[ {} : {} ]"));

            // whitespace test
            var parsed = Json.Parse("[\t\n{}\r\t]");
            Assert.IsTrue(parsed is IList);
            var parsedList = parsed as IList;
            Assert.IsTrue(parsedList[0] is IDictionary);

            parsed = Json.Parse("[ {}, { \"final\" : \"fantasy\"} ]");
            Assert.IsTrue(parsed is IList);
            parsedList = parsed as IList;
            Assert.IsTrue(parsedList[0] is IDictionary);
            Assert.IsTrue(parsedList[1] is IDictionary);
            var parsedDictionary = parsedList[1] as IDictionary;
            Assert.AreEqual("fantasy", parsedDictionary["final"]);
        }

        [TestMethod]
        public void TestJsonObjectWithElements()
        {
            // Just make sure they don't throw exception as we already check their content correctness
            // in other unit tests.
            Json.Parse("{ \"mura\": \"masa\" }");
            Json.Parse("{ \"mura\": 1234 }");
            Json.Parse("{ \"mura\": { \"masa\": 1234 } }");
            Json.Parse("{ \"mura\": { \"masa\": [ 1234 ] } }");
            Json.Parse("{ \"mura\": { \"masa\": [ 1234 ] }, \"arr\": [] }");
        }

        [TestMethod]
        public void TestJsonArrayWithElements()
        {
            // Just make sure they don't throw exception as we already check their content correctness
            // in other unit tests.
            Json.Parse("[ \"mura\" ]");
            Json.Parse("[ \"\u1234\" ]");
            Json.Parse("[ \"\u1234ff\", \"\u1234\" ]");
            Json.Parse("[ [], [], [], [] ]");
            Json.Parse("[ [], [ {}, {} ], [ {} ], [] ]");
        }

        [TestMethod]
        public void TestEncodeJson()
        {
            var dict = new Dictionary<string, object>();
            string encoded = Json.Encode(dict);
            Assert.AreEqual("{}", encoded);

            var list = new List<object>();
            encoded = Json.Encode(list);
            Assert.AreEqual("[]", encoded);

            var dictChild = new Dictionary<string, object>();
            list.Add(dictChild);
            encoded = Json.Encode(list);
            Assert.AreEqual("[{}]", encoded);

            list.Add("1234          a\t\r\n");
            list.Add(1234);
            list.Add(12.34);
            list.Add(1.23456789123456789);
            encoded = Json.Encode(list);
            Assert.AreEqual("[{},\"1234          a\\t\\r\\n\",1234,12.34,1.23456789123457]", encoded);

            dict["arr"] = new List<object>();
            encoded = Json.Encode(dict);
            Assert.AreEqual("{\"arr\":[]}", encoded);

            dict["\u1234"] = "\u1234";
            encoded = Json.Encode(dict);
            Assert.AreEqual("{\"arr\":[],\"\u1234\":\"\u1234\"}", encoded);

            encoded = Json.Encode(new List<object> { true, false, null });
            Assert.AreEqual("[true,false,null]", encoded);
        }

        [TestMethod]
        public void TestSpecialJsonNumbersAndModifiers()
        {
            Assert.ThrowsException<ArgumentException>(() => Json.Parse("+123456789"));

            Json.Parse("{ \"mura\": -123456789123456789 }");
            Json.Parse("{ \"mura\": 1.1234567891234567E308 }");
            Json.Parse("{ \"PI\": 3.141e-10 }");
            Json.Parse("{ \"PI\": 3.141E-10 }");

            Assert.AreEqual(123456789123456789, (Json.Parse("{ \"mura\": 123456789123456789 }") as IDictionary)["mura"]);
        }
    }
}
