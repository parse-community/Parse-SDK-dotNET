using Moq;
using NUnit.Framework;
using LeanCloud;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;

// TODO (hallucinogen): mock AVACL, AVObject, AVUser once we have their Interfaces
namespace ParseTest {
  [TestFixture]
  public class EncoderTests {
    /// <summary>
    /// A <see cref="ParseEncoder"/> that's used only for testing. This class is used to test
    /// <see cref="ParseEncoder"/>'s base methods.
    /// </summary>
    private class ParseEncoderTestClass : AVEncoder {
      private static readonly ParseEncoderTestClass instance = new ParseEncoderTestClass();
      public static ParseEncoderTestClass Instance {
        get {
          return instance;
        }
      }

      protected override IDictionary<string, object> EncodeParseObject(AVObject value) {
        return null;
      }
    }

    [Test]
    public void TestIsValidType() {
      var corgi = new AVObject("Corgi");
      var corgiRelation = corgi.GetRelation<AVObject>("corgi");

      Assert.IsTrue(AVEncoder.IsValidType(322));
      Assert.IsTrue(AVEncoder.IsValidType(0.3f));
      Assert.IsTrue(AVEncoder.IsValidType(new byte[]{ 1, 2, 3, 4 }));
      Assert.IsTrue(AVEncoder.IsValidType("corgi"));
      Assert.IsTrue(AVEncoder.IsValidType(corgi));
      Assert.IsTrue(AVEncoder.IsValidType(new AVACL()));
      Assert.IsTrue(AVEncoder.IsValidType(new AVFile("Corgi", new byte[0])));
      Assert.IsTrue(AVEncoder.IsValidType(new AVGeoPoint(1, 2)));
      Assert.IsTrue(AVEncoder.IsValidType(corgiRelation));
      Assert.IsTrue(AVEncoder.IsValidType(new DateTime()));
      Assert.IsTrue(AVEncoder.IsValidType(new List<object>()));
      Assert.IsTrue(AVEncoder.IsValidType(new Dictionary<string, string>()));
      Assert.IsTrue(AVEncoder.IsValidType(new Dictionary<string, object>()));

      Assert.IsFalse(AVEncoder.IsValidType(new AVAddOperation(new List<object>())));
      Assert.IsFalse(AVEncoder.IsValidType(Task<AVObject>.FromResult(new AVObject("Corgi"))));
      Assert.Throws<MissingMethodException>(() => AVEncoder.IsValidType(new Dictionary<object, object>()));
      Assert.Throws<MissingMethodException>(() => AVEncoder.IsValidType(new Dictionary<object, string>()));
    }

    [Test]
    public void TestEncodeDate() {
      DateTime dateTime = new DateTime(1990, 8, 30, 12, 3, 59);
      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(dateTime) as IDictionary<string, object>;
      Assert.AreEqual("Date", value["__type"]);
      Assert.AreEqual("1990-08-30T12:03:59.000Z", value["iso"]);
    }

    [Test]
    public void TestEncodeBytes() {
      byte[] bytes = new byte[] { 1, 2, 3, 4 };
      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(bytes) as IDictionary<string, object>;
      Assert.AreEqual("Bytes", value["__type"]);
      Assert.AreEqual(Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }), value["base64"]);
    }

    [Test]
    public void TestEncodeParseObjectWithNoObjectsEncoder() {
      AVObject obj = new AVObject("Corgi");
      Assert.Throws<ArgumentException>(() => NoObjectsEncoder.Instance.Encode(obj));
    }

    [Test]
    public void TestEncodeParseObjectWithPointerOrLocalIdEncoder() {
      // TODO (hallucinogen): we can't make an object with ID without saving for now. Let's revisit this after we make IParseObject
    }

    [Test]
    public void TestEncodeParseFile() {
      AVFile file1 = AVFileExtensions.Create("Corgi.png", new Uri("http://corgi.xyz/gogo.png"));
      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(file1) as IDictionary<string, object>;
      Assert.AreEqual("File", value["__type"]);
      Assert.AreEqual("Corgi.png", value["name"]);
      Assert.AreEqual("http://corgi.xyz/gogo.png", value["url"]);

      AVFile file2 = new AVFile(null, new MemoryStream(new byte[] { 1, 2, 3, 4 }));
      Assert.Throws<InvalidOperationException>(() => ParseEncoderTestClass.Instance.Encode(file2));
    }

    [Test]
    public void TestEncodeParseGeoPoint() {
      AVGeoPoint point = new AVGeoPoint(3.22, 32.2);
      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(point) as IDictionary<string, object>;
      Assert.AreEqual("GeoPoint", value["__type"]);
      Assert.AreEqual(3.22, value["latitude"]);
      Assert.AreEqual(32.2, value["longitude"]);
    }

    [Test]
    public void TestEncodeACL() {
      AVACL acl1 = new AVACL();
      IDictionary<string, object> value1 = ParseEncoderTestClass.Instance.Encode(acl1) as IDictionary<string, object>;
      Assert.IsNotNull(value1);
      Assert.AreEqual(0, value1.Keys.Count);

      AVACL acl2 = new AVACL();
      acl2.PublicReadAccess = true;
      acl2.PublicWriteAccess = true;
      IDictionary<string, object> value2 = ParseEncoderTestClass.Instance.Encode(acl2) as IDictionary<string, object>;
      Assert.AreEqual(1, value2.Keys.Count);
      IDictionary<string, object> publicAccess = value2["*"] as IDictionary<string, object>;
      Assert.AreEqual(2, publicAccess.Keys.Count);
      Assert.IsTrue((bool)publicAccess["read"]);
      Assert.IsTrue((bool)publicAccess["write"]);

      // TODO (hallucinogen): mock AVUser and test SetReadAccess and SetWriteAccess
    }

    [Test]
    public void TestEncodeParseRelation() {
      var obj = new AVObject("Corgi");
      AVRelation<AVObject> relation = AVRelationExtensions.Create<AVObject>(obj, "nano", "Husky");
      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(relation) as IDictionary<string, object>;
      Assert.AreEqual("Relation", value["__type"]);
      Assert.AreEqual("Husky", value["className"]);
    }

    [Test]
    public void TestEncodeParseFieldOperation() {
      var incOps = new AVIncrementOperation(1);
      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(incOps) as IDictionary<string, object>;
      Assert.AreEqual("Increment", value["__op"]);
      Assert.AreEqual(1, value["amount"]);
      // Other operations are tested in FieldOperationTests
    }

    [Test]
    public void TestEncodeList() {
      IList<object> list = new List<object>();
      list.Add(new AVGeoPoint(0, 0));
      list.Add("item");
      list.Add(new byte[] { 1, 2, 3, 4 });
      list.Add(new string[] { "hikaru", "hanatan", "ultimate" });
      list.Add(new Dictionary<string, object>() {
        { "elements", new int[] { 1, 2, 3 } },
        { "mystic", "cage" },
        { "listAgain", new List<object>() { "xilia", "zestiria", "symphonia" } }
      });

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

    [Test]
    public void TestEncodeDictionary() {
      IDictionary<string, object> dict = new Dictionary<string, object>() {
        { "item", "random" },
        { "list", new List<object>(){ "vesperia", "abyss", "legendia" } },
        { "array", new int[] { 1, 2, 3 } },
        { "geo", new AVGeoPoint(0, 0) },
        { "validDict", new Dictionary<string, object>(){ { "phantasia", "jbf" } } }
      };

      IDictionary<string, object> value = ParseEncoderTestClass.Instance.Encode(dict) as IDictionary<string, object>;
      Assert.AreEqual("random", value["item"]);
      Assert.IsTrue(value["list"] is IList<object>);
      Assert.IsTrue(value["array"] is IList<object>);
      Assert.IsTrue(value["geo"] is IDictionary<string, object>);
      Assert.IsTrue(value["validDict"] is IDictionary<string, object>);

      IDictionary<object, string> invalidDict = new Dictionary<object, string>();
      Assert.Throws<MissingMethodException>(() => ParseEncoderTestClass.Instance.Encode(invalidDict));

      IDictionary<string, object> childInvalidDict = new Dictionary<string, object>() {
         { "validDict", new Dictionary<object, string>(){ { new AVACL(), "jbf" } } }
      };
      Assert.Throws<MissingMethodException>(() => ParseEncoderTestClass.Instance.Encode(childInvalidDict));
    }
  }
}
