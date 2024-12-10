using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Internal;
using Parse.Infrastructure;
using Parse.Infrastructure.Control;
using Parse.Infrastructure.Data;

// TODO (hallucinogen): mock ParseACL, ParseObject, ParseUser once we have their Interfaces
namespace Parse.Tests;

[TestClass]
public class EncoderTests
{
    ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    /// <summary>
    /// A <see cref="ParseDataEncoder"/> that's used only for testing. This class is used to test
    /// <see cref="ParseDataEncoder"/>'s base methods.
    /// </summary>
    class ParseEncoderTestClass : ParseDataEncoder
    {
        public static ParseEncoderTestClass Instance { get; } = new ParseEncoderTestClass { };

        protected override IDictionary<string, object> EncodeObject(ParseObject value) => null;
    }

    [TestMethod]
    public void TestIsValidType()
    {
        ParseObject corgi = new ParseObject("Corgi");
        ParseRelation<ParseObject> corgiRelation = corgi.GetRelation<ParseObject>(nameof(corgi));

        Assert.IsTrue(ParseDataEncoder.Validate(322));
        Assert.IsTrue(ParseDataEncoder.Validate(0.3f));
        Assert.IsTrue(ParseDataEncoder.Validate(new byte[] { 1, 2, 3, 4 }));
        Assert.IsTrue(ParseDataEncoder.Validate(nameof(corgi)));
        Assert.IsTrue(ParseDataEncoder.Validate(corgi));
        Assert.IsTrue(ParseDataEncoder.Validate(new ParseACL { }));
        Assert.IsTrue(ParseDataEncoder.Validate(new ParseFile("Corgi", new byte[0])));
        Assert.IsTrue(ParseDataEncoder.Validate(new ParseGeoPoint(1, 2)));
        Assert.IsTrue(ParseDataEncoder.Validate(corgiRelation));
        Assert.IsTrue(ParseDataEncoder.Validate(new DateTime { }));
        Assert.IsTrue(ParseDataEncoder.Validate(new List<object> { }));
        Assert.IsTrue(ParseDataEncoder.Validate(new Dictionary<string, string> { }));
        Assert.IsTrue(ParseDataEncoder.Validate(new Dictionary<string, object> { }));

        Assert.IsFalse(ParseDataEncoder.Validate(new ParseAddOperation(new List<object> { })));
        Assert.IsFalse(ParseDataEncoder.Validate(Task.FromResult(new ParseObject("Corgi"))));
        Assert.ThrowsException<MissingMethodException>(() => ParseDataEncoder.Validate(new Dictionary<object, object> { }));
        Assert.ThrowsException<MissingMethodException>(() => ParseDataEncoder.Validate(new Dictionary<object, string> { }));
    }

    [TestMethod]
    public void TestEncodeDate()
    {
        DateTime dateTime = new DateTime(1990, 8, 30, 12, 3, 59);

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(dateTime, Client) as IDictionary<string, object>;

        Assert.AreEqual("Date", value["__type"]);
        Assert.AreEqual("1990-08-30T12:03:59.000Z", value["iso"]);
    }

    [TestMethod]
    public void TestEncodeBytes()
    {
        byte[] bytes = new byte[] { 1, 2, 3, 4 };

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(bytes, Client) as IDictionary<string, object>;

        Assert.AreEqual("Bytes", value["__type"]);
        Assert.AreEqual(Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }), value["base64"]);
    }

    [TestMethod]
    public void TestEncodeParseObjectWithNoObjectsEncoder()
    {
        ParseObject obj = new ParseObject("Corgi");

        Assert.ThrowsException<ArgumentException>(() => NoObjectsEncoder.Instance.Encode(obj, Client));
    }

    [TestMethod]
    public void TestEncodeParseObjectWithPointerOrLocalIdEncoder()
    {
        // TODO (hallucinogen): we can't make an object with ID without saving for now. Let's revisit this after we make IParseObject
    }

    [TestMethod]
    public void TestEncodeParseFile()
    {
        ParseFile file1 = ParseFileExtensions.Create("Corgi.png", new Uri("http://corgi.xyz/gogo.png"));

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(file1, Client) as IDictionary<string, object>;

        Assert.AreEqual("File", value["__type"]);
        Assert.AreEqual("Corgi.png", value["name"]);
        Assert.AreEqual("http://corgi.xyz/gogo.png", value["url"]);

        ParseFile file2 = new ParseFile(null, new MemoryStream(new byte[] { 1, 2, 3, 4 }));

        Assert.ThrowsException<InvalidOperationException>(() => ParseEncoderTestClass.Instance.Encode(file2, Client));
    }

    [TestMethod]
    public void TestEncodeParseGeoPoint()
    {
        ParseGeoPoint point = new ParseGeoPoint(3.22, 32.2);

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(point, Client) as IDictionary<string, object>;

        Assert.AreEqual("GeoPoint", value["__type"]);
        Assert.AreEqual(3.22, value["latitude"]);
        Assert.AreEqual(32.2, value["longitude"]);
    }

    [TestMethod]
    public void TestEncodeACL()
    {
        ParseACL acl1 = new ParseACL();

        IDictionary<string, object> value1 = ParseEncoderTestClass.Instance.Encode(acl1, Client) as IDictionary<string, object>;

        Assert.IsNotNull(value1);
        Assert.AreEqual(0, value1.Keys.Count);

        ParseACL acl2 = new ParseACL
        {
            PublicReadAccess = true,
            PublicWriteAccess = true
        };

        IDictionary<string, object> value2 = ParseEncoderTestClass.Instance.Encode(acl2, Client) as IDictionary<string, object>;

        Assert.AreEqual(1, value2.Keys.Count);
        IDictionary<string, object> publicAccess = value2["*"] as IDictionary<string, object>;
        Assert.AreEqual(2, publicAccess.Keys.Count);
        Assert.IsTrue((bool) publicAccess["read"]);
        Assert.IsTrue((bool) publicAccess["write"]);

        // TODO (hallucinogen): mock ParseUser and test SetReadAccess and SetWriteAccess
    }

    [TestMethod]
    public void TestEncodeParseRelation()
    {
        ParseObject obj = new ParseObject("Corgi");
        ParseRelation<ParseObject> relation = ParseRelationExtensions.Create<ParseObject>(obj, "nano", "Husky");

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(relation, Client) as IDictionary<string, object>;

        Assert.AreEqual("Relation", value["__type"]);
        Assert.AreEqual("Husky", value["className"]);
    }

    [TestMethod]
    public void TestEncodeParseFieldOperation()
    {
        ParseIncrementOperation incOps = new ParseIncrementOperation(1);

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(incOps, Client) as IDictionary<string, object>;

        Assert.AreEqual("Increment", value["__op"]);
        Assert.AreEqual(1, value["amount"]);

        // Other operations are tested in FieldOperationTests.
    }

    [TestMethod]
    public void TestEncodeList()
    {
        IList<object> list = new List<object>
    {
        new ParseGeoPoint(0, 0),
        "item",
        new byte[] { 1, 2, 3, 4 },
        new string[] { "hikaru", "hanatan", "ultimate" },
        new Dictionary<string, object>
        {
            ["elements"] = new int[] { 1, 2, 3 },
            ["mystic"] = "cage",
            ["listAgain"] = new List<object> { "xilia", "zestiria", "symphonia" }
        }
    };

        Debug.WriteLine($"Original list: {list}");

        IList<object> value = ParseEncoderTestClass.Instance.Encode(list, Client) as IList<object>;

        Assert.IsNotNull(value);

        // Validate ParseGeoPoint
        IDictionary<string, object> item0 = value[0] as IDictionary<string, object>;
        Assert.IsNotNull(item0);
        Assert.AreEqual("GeoPoint", item0["__type"]);
        Assert.AreEqual(0.0, item0["latitude"]);
        Assert.AreEqual(0.0, item0["longitude"]);

        // Validate string
        Assert.AreEqual("item", value[1]);

        // Validate byte[]
        IDictionary<string, object> item2 = value[2] as IDictionary<string, object>;
        Debug.WriteLine($"Encoded item2: {item2}, Type: {item2?.GetType()}");
        Assert.IsNotNull(item2);
        Assert.AreEqual("Bytes", item2["__type"]);
        Assert.AreEqual("AQIDBA==", item2["base64"]); // Base64 representation of {1,2,3,4}

        // Validate string[]
        IList<object> item3 = value[3] as IList<object>;
        Assert.IsNotNull(item3);
        Assert.AreEqual("hikaru", item3[0]);
        Assert.AreEqual("hanatan", item3[1]);
        Assert.AreEqual("ultimate", item3[2]);

        // Validate nested dictionary
        IDictionary<string, object> item4 = value[4] as IDictionary<string, object>;
        Assert.IsNotNull(item4);
        Assert.IsTrue(item4["elements"] is IList<object>);
        Assert.AreEqual("cage", item4["mystic"]);
        Assert.IsTrue(item4["listAgain"] is IList<object>);
    }


    [TestMethod]
    public void TestEncodeDictionary()
    {
        IDictionary<string, object> dict = new Dictionary<string, object>
        {
            ["item"] = "random",
            ["list"] = new List<object> { "vesperia", "abyss", "legendia" },
            ["array"] = new int[] { 1, 2, 3 },
            ["geo"] = new ParseGeoPoint(0, 0),
            ["validDict"] = new Dictionary<string, object> { ["phantasia"] = "jbf" }
        };

        IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(dict, Client) as IDictionary<string, object>;

        Assert.IsNotNull(value);
        Assert.AreEqual("random", value["item"]);
        Assert.IsTrue(value["list"] is IList<object>);
        Assert.IsTrue(value["array"] is IList<object>);
        Assert.IsTrue(value["geo"] is IDictionary<string, object>);
        Assert.IsTrue(value["validDict"] is IDictionary<string, object>);

        Assert.ThrowsException<ArgumentException>(() =>
            ParseEncoderTestClass.Instance.Encode(new Dictionary<object, string>(), Client));

        Assert.ThrowsException<ArgumentException>(() =>
            ParseEncoderTestClass.Instance.Encode(new Dictionary<string, object>
            {
                ["validDict"] = new Dictionary<object, string> { [new ParseACL()] = "jbf" }
            }, Client));
    }

}
