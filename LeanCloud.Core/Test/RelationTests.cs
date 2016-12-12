using System.Collections.Generic;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;

namespace ParseTest {
  [TestFixture]
  public class RelationTests {
    [Test]
    public void TestRelationQuery() {
      AVObject parent = AVObject.CreateWithoutData("Foo", "abcxyz");

      AVRelation<AVObject> relation = parent.GetRelation<AVObject>("child");
      AVQuery<AVObject> query = relation.Query;

      // Client side, the query will appear to be for the wrong class.
      // When the server recieves it, the class name will be redirected using the 'redirectClassNameForKey' option.
      Assert.AreEqual("Foo", query.GetClassName());

      IDictionary<string, object> encoded = query.BuildParameters();

      Assert.AreEqual("child", encoded["redirectClassNameForKey"]);
    }
  }
}