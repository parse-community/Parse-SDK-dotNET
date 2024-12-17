using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Internal;
using Parse.Infrastructure;
using Parse.Infrastructure.Data;

namespace Parse.Tests;

[TestClass]
public class DecoderTests
{
    ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    [TestMethod]
    public void TestParseDate()
    {
        DateTime dateTime = (DateTime) Client.Decoder.Decode(ParseDataDecoder.ParseDate("1990-08-30T12:03:59.000Z"), Client);

        Assert.AreEqual(1990, dateTime.Year);
        Assert.AreEqual(8, dateTime.Month);
        Assert.AreEqual(30, dateTime.Day);
        Assert.AreEqual(12, dateTime.Hour);
        Assert.AreEqual(3, dateTime.Minute);
        Assert.AreEqual(59, dateTime.Second);
        Assert.AreEqual(0, dateTime.Millisecond);
    }

    [TestMethod]
    public void TestDecodePrimitives()
    {
        Assert.AreEqual(1, Client.Decoder.Decode(1, Client));
        Assert.AreEqual(0.3, Client.Decoder.Decode(0.3, Client));
        Assert.AreEqual("halyosy", Client.Decoder.Decode("halyosy", Client));

        Assert.IsNull(Client.Decoder.Decode(default, Client));
    }

    [TestMethod]
    // Decoding ParseFieldOperation is not supported on .NET now. We only need this for LDS.
    public void TestDecodeFieldOperation() => Assert.ThrowsException<NotImplementedException>(() => Client.Decoder.Decode(new Dictionary<string, object> { { "__op", "Increment" }, { "amount", "322" } }, Client));

    [TestMethod]
    public void TestDecodeDate()
    {
        DateTime dateTime = (DateTime) Client.Decoder.Decode(new Dictionary<string, object> { { "__type", "Date" }, { "iso", "1990-08-30T12:03:59.000Z" } }, Client);

        Assert.AreEqual(1990, dateTime.Year);
        Assert.AreEqual(8, dateTime.Month);
        Assert.AreEqual(30, dateTime.Day);
        Assert.AreEqual(12, dateTime.Hour);
        Assert.AreEqual(3, dateTime.Minute);
        Assert.AreEqual(59, dateTime.Second);
        Assert.AreEqual(0, dateTime.Millisecond);
    }

    [TestMethod]
    public void TestDecodeImproperDate()
    {
        IDictionary<string, object> value = new Dictionary<string, object> { ["__type"] = "Date", ["iso"] = "1990-08-30T12:03:59.0Z" };

        for (int i = 0; i < 2; i++, value["iso"] = (value["iso"] as string).Substring(0, (value["iso"] as string).Length - 1) + "0Z")
        {
            DateTime dateTime = (DateTime) Client.Decoder.Decode(value, Client);

            Assert.AreEqual(1990, dateTime.Year);
            Assert.AreEqual(8, dateTime.Month);
            Assert.AreEqual(30, dateTime.Day);
            Assert.AreEqual(12, dateTime.Hour);
            Assert.AreEqual(3, dateTime.Minute);
            Assert.AreEqual(59, dateTime.Second);
            Assert.AreEqual(0, dateTime.Millisecond);
        }
    }

    [TestMethod]
    public void TestDecodeBytes() => Assert.AreEqual("This is an encoded string", System.Text.Encoding.UTF8.GetString(Client.Decoder.Decode(new Dictionary<string, object> { { "__type", "Bytes" }, { "base64", "VGhpcyBpcyBhbiBlbmNvZGVkIHN0cmluZw==" } }, Client) as byte[]));

    [TestMethod]
    public void TestDecodePointer()
    {
        ParseObject obj = Client.Decoder.Decode(new Dictionary<string, object> { ["__type"] = "Pointer", ["className"] = "Corgi", ["objectId"] = "lLaKcolnu" }, Client) as ParseObject;

        Assert.IsFalse(obj.IsDataAvailable);
        Assert.AreEqual("Corgi", obj.ClassName);
        Assert.AreEqual("lLaKcolnu", obj.ObjectId);
    }

    [TestMethod]
    public void TestDecodeFile()
    {

        ParseFile file1 = Client.Decoder.Decode(new Dictionary<string, object> { ["__type"] = "File", ["name"] = "parsee.png", ["url"] = "https://user-images.githubusercontent.com/5673677/138278489-7d0cebc5-1e31-4d3c-8ffb-53efcda6f29d.png" }, Client) as ParseFile;

        Assert.AreEqual("parsee.png", file1.Name);
        Assert.AreEqual("https://user-images.githubusercontent.com/5673677/138278489-7d0cebc5-1e31-4d3c-8ffb-53efcda6f29d.png", file1.Url.AbsoluteUri);
        Assert.IsFalse(file1.IsDirty);

        Assert.ThrowsException<KeyNotFoundException>(() => Client.Decoder.Decode(new Dictionary<string, object> { ["__type"] = "File", ["name"] = "Corgi.png" }, Client));
    }

    [TestMethod]
    public void TestDecodeGeoPoint()
    {
        ParseGeoPoint point1 = (ParseGeoPoint) Client.Decoder.Decode(new Dictionary<string, object> { ["__type"] = "GeoPoint", ["latitude"] = 0.9, ["longitude"] = 0.3 }, Client);

        Assert.IsNotNull(point1);
        Assert.AreEqual(0.9, point1.Latitude);
        Assert.AreEqual(0.3, point1.Longitude);

        Assert.ThrowsException<KeyNotFoundException>(() => Client.Decoder.Decode(new Dictionary<string, object> { ["__type"] = "GeoPoint", ["latitude"] = 0.9 }, Client));
    }

    [TestMethod]
    public void TestDecodeObject()
    {
        IDictionary<string, object> value = new Dictionary<string, object>()
        {
            ["__type"] = "Object",
            ["className"] = "Corgi",
            ["objectId"] = "lLaKcolnu",
            ["createdAt"] = "2015-06-22T21:23:41.733Z",
            ["updatedAt"] = "2015-06-22T22:06:41.733Z"
        };

        ParseObject obj = Client.Decoder.Decode(value, Client) as ParseObject;

        Assert.IsTrue(obj.IsDataAvailable);
        Assert.AreEqual("Corgi", obj.ClassName);
        Assert.AreEqual("lLaKcolnu", obj.ObjectId);
        Assert.IsNotNull(obj.CreatedAt);
        Assert.IsNotNull(obj.UpdatedAt);
    }

    [TestMethod]
    public void TestDecodeRelation()
    {
        IDictionary<string, object> value = new Dictionary<string, object>()
        {
            ["__type"] = "Relation",
            ["className"] = "Corgi",
            ["objectId"] = "lLaKcolnu"
        };

        ParseRelation<ParseObject> relation = Client.Decoder.Decode(value, Client) as ParseRelation<ParseObject>;

        Assert.IsNotNull(relation);
        Assert.AreEqual("Corgi", relation.GetTargetClassName());
    }

    [TestMethod]
    public void TestDecodeDictionary()
    {
        IDictionary<string, object> value = new Dictionary<string, object>()
        {
            ["megurine"] = "luka",
            ["hatsune"] = new ParseObject("Miku"),
            ["decodedGeoPoint"] = new Dictionary<string, object>
            {
                ["__type"] = "GeoPoint",
                ["latitude"] = 0.9,
                ["longitude"] = 0.3
            },
            ["listWithSomething"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["__type"] = "GeoPoint",
                    ["latitude"] = 0.9,
                    ["longitude"] = 0.3
                }
            }
        };

        IDictionary<string, object> dict = Client.Decoder.Decode(value, Client) as IDictionary<string, object>;

        Assert.AreEqual("luka", dict["megurine"]);
        Assert.IsTrue(dict["hatsune"] is ParseObject);
        Assert.IsTrue(dict["decodedGeoPoint"] is ParseGeoPoint);
        Assert.IsTrue(dict["listWithSomething"] is IList<object>);
        IList<object> decodedList = dict["listWithSomething"] as IList<object>;
        Assert.IsTrue(decodedList[0] is ParseGeoPoint);

        IDictionary<object, string> randomValue = new Dictionary<object, string>()
        {
            ["ultimate"] = "elements",
            [new ParseACL { }] = "lLaKcolnu"
        };

        IDictionary<object, string> randomDict = Client.Decoder.Decode(randomValue, Client) as IDictionary<object, string>;

        Assert.AreEqual("elements", randomDict["ultimate"]);
        Assert.AreEqual(2, randomDict.Keys.Count);
    }

    [TestMethod]
    public void TestDecodeList()
    {
        IList<object> value = new List<object>
        {
            1, new ParseACL { }, "wiz",
            new Dictionary<string, object>
            {
                ["__type"] = "GeoPoint",
                ["latitude"] = 0.9,
                ["longitude"] = 0.3
            },
            new List<object>
            {
                new Dictionary<string, object>
                {
                    ["__type"] = "GeoPoint",
                    ["latitude"] =  0.9,
                    ["longitude"] = 0.3
                }
            }
        };

        IList<object> list = Client.Decoder.Decode(value, Client) as IList<object>;

        Assert.AreEqual(1, list[0]);
        Assert.IsTrue(list[1] is ParseACL);
        Assert.AreEqual("wiz", list[2]);
        Assert.IsTrue(list[3] is ParseGeoPoint);
        Assert.IsTrue(list[4] is IList<object>);
        IList<object> decodedList = list[4] as IList<object>;
        Assert.IsTrue(decodedList[0] is ParseGeoPoint);
    }

    [TestMethod]
    public void TestDecodeArray()
    {
        int[] value = new int[] { 1, 2, 3, 4 }, array = Client.Decoder.Decode(value, Client) as int[];

        Assert.AreEqual(4, array.Length);
        Assert.AreEqual(1, array[0]);
        Assert.AreEqual(2, array[1]);
    }
}
