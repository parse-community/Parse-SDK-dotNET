using Parse;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

// TODO (hallucinogen): mock ParseACL, ParseObject, ParseUser once we have their Interfaces
namespace Parse.Test
{
    [TestClass]
    public class EncoderTests
    {
        /// <summary>
        /// A <see cref="ParseEncoder"/> that's used only for testing. This class is used to test
        /// <see cref="ParseEncoder"/>'s base methods.
        /// </summary>
        private class ParseEncoderTestClass : ParseEncoder
        {
            private static readonly ParseEncoderTestClass instance = new ParseEncoderTestClass();
            public static ParseEncoderTestClass Instance => instance;

            protected override IDictionary<string, object> EncodeParseObject(ParseObject value) => null;
        }

        [TestMethod]
        public void TestIsValidType()
        {
            var corgi = new ParseObject("Corgi");
            var corgiRelation = corgi.GetRelation<ParseObject>("corgi");
            
            Assert.IsTrue(ParseEncoder.IsValidType(322));
            Assert.IsTrue(ParseEncoder.IsValidType(0.3f));
            Assert.IsTrue(ParseEncoder.IsValidType(new byte[] { 1, 2, 3, 4 }));
            Assert.IsTrue(ParseEncoder.IsValidType("corgi"));
            Assert.IsTrue(ParseEncoder.IsValidType(corgi));
            Assert.IsTrue(ParseEncoder.IsValidType(new ParseACL()));
            Assert.IsTrue(ParseEncoder.IsValidType(new ParseFile("Corgi", new byte[0])));
            Assert.IsTrue(ParseEncoder.IsValidType(new ParseGeoPoint(1, 2)));
            Assert.IsTrue(ParseEncoder.IsValidType(corgiRelation));
            Assert.IsTrue(ParseEncoder.IsValidType(new DateTime()));
            Assert.IsTrue(ParseEncoder.IsValidType(new List<object>()));
            Assert.IsTrue(ParseEncoder.IsValidType(new Dictionary<string, string>()));
            Assert.IsTrue(ParseEncoder.IsValidType(new Dictionary<string, object>()));

            Assert.IsFalse(ParseEncoder.IsValidType(new ParseAddOperation(new List<object>())));
            Assert.IsFalse(ParseEncoder.IsValidType(Task.FromResult(new ParseObject("Corgi"))));
            Assert.ThrowsException<MissingMethodException>(() => ParseEncoder.IsValidType(new Dictionary<object, object>()));
            Assert.ThrowsException<MissingMethodException>(() => ParseEncoder.IsValidType(new Dictionary<object, string>()));
        }

        [TestMethod]
        public void TestEncodeDate()
        {
            DateTime dateTime = new DateTime(1990, 8, 30, 12, 3, 59);
            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(dateTime) as IDictionary<string, object>;
            Assert.AreEqual("Date", value["__type"]);
            Assert.AreEqual("1990-08-30T12:03:59.000Z", value["iso"]);
        }

        [TestMethod]
        public void TestEncodeBytes()
        {
            byte[] bytes = new byte[] { 1, 2, 3, 4 };
            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(bytes) as IDictionary<string, object>;
            Assert.AreEqual("Bytes", value["__type"]);
            Assert.AreEqual(Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }), value["base64"]);
        }

        [TestMethod]
        public void TestEncodeParseObjectWithNoObjectsEncoder()
        {
            ParseObject obj = new ParseObject("Corgi");
            Assert.ThrowsException<ArgumentException>(() => NoObjectsEncoder.Instance.Encode(obj));
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
            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(file1) as IDictionary<string, object>;
            Assert.AreEqual("File", value["__type"]);
            Assert.AreEqual("Corgi.png", value["name"]);
            Assert.AreEqual("http://corgi.xyz/gogo.png", value["url"]);

            ParseFile file2 = new ParseFile(null, new MemoryStream(new byte[] { 1, 2, 3, 4 }));
            Assert.ThrowsException<InvalidOperationException>(() => ParseEncoderTestClass.Instance.Encode(file2));
        }

        [TestMethod]
        public void TestEncodeParseGeoPoint()
        {
            ParseGeoPoint point = new ParseGeoPoint(3.22, 32.2);
            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(point) as IDictionary<string, object>;
            Assert.AreEqual("GeoPoint", value["__type"]);
            Assert.AreEqual(3.22, value["latitude"]);
            Assert.AreEqual(32.2, value["longitude"]);
        }

        [TestMethod]
        public void TestEncodeACL()
        {
            ParseACL acl1 = new ParseACL();
            IDictionary<string, object> value1 = ParseEncoderTestClass.Instance.Encode(acl1) as IDictionary<string, object>;
            Assert.IsNotNull(value1);
            Assert.AreEqual(0, value1.Keys.Count);

            ParseACL acl2 = new ParseACL
            {
                PublicReadAccess = true,
                PublicWriteAccess = true
            };
            IDictionary<string, object> value2 = ParseEncoderTestClass.Instance.Encode(acl2) as IDictionary<string, object>;
            Assert.AreEqual(1, value2.Keys.Count);
            IDictionary<string, object> publicAccess = value2["*"] as IDictionary<string, object>;
            Assert.AreEqual(2, publicAccess.Keys.Count);
            Assert.IsTrue((bool)publicAccess["read"]);
            Assert.IsTrue((bool)publicAccess["write"]);

            // TODO (hallucinogen): mock ParseUser and test SetReadAccess and SetWriteAccess
        }

        [TestMethod]
        public void TestEncodeParseRelation()
        {
            var obj = new ParseObject("Corgi");
            ParseRelation<ParseObject> relation = ParseRelationExtensions.Create<ParseObject>(obj, "nano", "Husky");
            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(relation) as IDictionary<string, object>;
            Assert.AreEqual("Relation", value["__type"]);
            Assert.AreEqual("Husky", value["className"]);
        }

        [TestMethod]
        public void TestEncodeParseFieldOperation()
        {
            var incOps = new ParseIncrementOperation(1);
            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(incOps) as IDictionary<string, object>;
            Assert.AreEqual("Increment", value["__op"]);
            Assert.AreEqual(1, value["amount"]);
            // Other operations are tested in FieldOperationTests
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
                new Dictionary<string, object>()
                {
                    { "elements", new int[] { 1, 2, 3 } },
                    { "mystic", "cage" },
                    { "listAgain", new List<object>() { "xilia", "zestiria", "symphonia" } }
                }
            };

            IList<object> value = ParseEncoderTestClass.Instance.Encode(list) as IList<object>;
            var item0 = value[0] as IDictionary<string, object>;
            Assert.AreEqual("GeoPoint", item0["__type"]);
            Assert.AreEqual(0.0, item0["latitude"]);
            Assert.AreEqual(0.0, item0["longitude"]);

            Assert.AreEqual("item", value[1]);

            var item2 = value[2] as IDictionary<string, object>;
            Assert.AreEqual("Bytes", item2["__type"]);

            var item3 = value[3] as IList<object>;
            Assert.AreEqual("hikaru", item3[0]);
            Assert.AreEqual("hanatan", item3[1]);
            Assert.AreEqual("ultimate", item3[2]);

            var item4 = value[4] as IDictionary<string, object>;
            Assert.IsTrue(item4["elements"] is IList<object>);
            Assert.AreEqual("cage", item4["mystic"]);
            Assert.IsTrue(item4["listAgain"] is IList<object>);
        }

        [TestMethod]
        public void TestEncodeDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>() {
        { "item", "random" },
        { "list", new List<object>(){ "vesperia", "abyss", "legendia" } },
        { "array", new int[] { 1, 2, 3 } },
        { "geo", new ParseGeoPoint(0, 0) },
        { "validDict", new Dictionary<string, object>(){ { "phantasia", "jbf" } } }
      };

            IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(dict) as IDictionary<string, object>;
            Assert.AreEqual("random", value["item"]);
            Assert.IsTrue(value["list"] is IList<object>);
            Assert.IsTrue(value["array"] is IList<object>);
            Assert.IsTrue(value["geo"] is IDictionary<string, object>);
            Assert.IsTrue(value["validDict"] is IDictionary<string, object>);

            IDictionary<object, string> invalidDict = new Dictionary<object, string>();
            Assert.ThrowsException<MissingMethodException>(() => ParseEncoderTestClass.Instance.Encode(invalidDict));

            IDictionary<string, object> childInvalidDict = new Dictionary<string, object>() {
         { "validDict", new Dictionary<object, string>(){ { new ParseACL(), "jbf" } } }
      };
            Assert.ThrowsException<MissingMethodException>(() => ParseEncoderTestClass.Instance.Encode(childInvalidDict));
        }
    }
}
