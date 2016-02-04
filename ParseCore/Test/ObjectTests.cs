using Moq;
using NUnit.Framework;
using Parse;
using Parse.Core.Internal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class ObjectTests {
    [ParseClassName("SubClass")]
    private class SubClass : ParseObject {
    }

    [ParseClassName("UnregisteredSubClass")]
    private class UnregisteredSubClass : ParseObject {
    }

    [TearDown]
    public void TearDown() {
      ParseCorePlugins.Instance.Reset();
    }

    [Test]
    public void TestParseObjectConstructor() {
      ParseObject obj = new ParseObject("Corgi");
      Assert.AreEqual("Corgi", obj.ClassName);
      Assert.Null(obj.CreatedAt);
      Assert.True(obj.IsDataAvailable);
      Assert.True(obj.IsDirty);
    }

    [Test]
    public void TestParseObjectCreate() {
      ParseObject obj = ParseObject.Create("Corgi");
      Assert.AreEqual("Corgi", obj.ClassName);
      Assert.Null(obj.CreatedAt);
      Assert.True(obj.IsDataAvailable);
      Assert.True(obj.IsDirty);

      ParseObject obj2 = ParseObject.CreateWithoutData("Corgi", "waGiManPutr4Pet1r");
      Assert.AreEqual("Corgi", obj2.ClassName);
      Assert.AreEqual("waGiManPutr4Pet1r", obj2.ObjectId);
      Assert.Null(obj2.CreatedAt);
      Assert.False(obj2.IsDataAvailable);
      Assert.False(obj2.IsDirty);
    }

    [Test]
    public void TestParseObjectCreateWithGeneric() {
      ParseObject.RegisterSubclass<SubClass>();

      ParseObject obj = ParseObject.Create<SubClass>();
      Assert.AreEqual("SubClass", obj.ClassName);
      Assert.Null(obj.CreatedAt);
      Assert.True(obj.IsDataAvailable);
      Assert.True(obj.IsDirty);

      ParseObject obj2 = ParseObject.CreateWithoutData<SubClass>("waGiManPutr4Pet1r");
      Assert.AreEqual("SubClass", obj2.ClassName);
      Assert.AreEqual("waGiManPutr4Pet1r", obj2.ObjectId);
      Assert.Null(obj2.CreatedAt);
      Assert.False(obj2.IsDataAvailable);
      Assert.False(obj2.IsDirty);
    }

    [Test]
    public void TestParseObjectCreateWithGenericFailWithoutSubclass() {
      Assert.Throws<InvalidCastException>(() => ParseObject.Create<SubClass>());
    }

    [Test]
    public void TestFromState() {
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");

      Assert.AreEqual("waGiManPutr4Pet1r", obj.ObjectId);
      Assert.AreEqual("Pagi", obj.ClassName);
      Assert.NotNull(obj.CreatedAt);
      Assert.Null(obj.UpdatedAt);
      Assert.AreEqual("kevin", obj["username"]);
      Assert.AreEqual("se551onT0k3n", obj["sessionToken"]);
    }

    [Test]
    public void TestRegisterSubclass() {
      Assert.Throws<InvalidCastException>(() => ParseObject.Create<SubClass>());

      ParseObject.RegisterSubclass<SubClass>();
      Assert.DoesNotThrow(() => ParseObject.Create<SubClass>());

      ParseCorePlugins.Instance.SubclassingController.UnregisterSubclass(typeof(UnregisteredSubClass));
      Assert.DoesNotThrow(() => ParseObject.Create<SubClass>());

      ParseCorePlugins.Instance.SubclassingController.UnregisterSubclass(typeof(SubClass));
      Assert.Throws<InvalidCastException>(() => ParseObject.Create<SubClass>());
    }

    [Test]
    public void TestRevert() {
      ParseObject obj = ParseObject.Create("Corgi");
      obj["gogo"] = true;

      Assert.True(obj.IsDirty);
      Assert.AreEqual(1, obj.GetCurrentOperations().Count);
      Assert.True(obj.ContainsKey("gogo"));

      obj.Revert();

      Assert.True(obj.IsDirty);
      Assert.AreEqual(0, obj.GetCurrentOperations().Count);
      Assert.False(obj.ContainsKey("gogo"));
    }

    [Test]
    public void TestDeepTraversal() {
      ParseObject obj = ParseObject.Create("Corgi");
      IDictionary<string, object> someDict = new Dictionary<string, object>() {
        { "someList", new List<object>() }
      };
      obj["obj"] = ParseObject.Create("Pug");
      obj["obj2"] = ParseObject.Create("Pug");
      obj["list"] = new List<object>();
      obj["dict"] = someDict;
      obj["someBool"] = true;
      obj["someInt"] = 23;

      IEnumerable<object> traverseResult = ParseObjectExtensions.DeepTraversal(obj, true, true);
      Assert.AreEqual(8, traverseResult.Count());

      // Don't traverse beyond the root (since root is ParseObject)
      traverseResult = ParseObjectExtensions.DeepTraversal(obj, false, true);
      Assert.AreEqual(1, traverseResult.Count());

      traverseResult = ParseObjectExtensions.DeepTraversal(someDict, false, true);
      Assert.AreEqual(2, traverseResult.Count());

      // Should ignore root
      traverseResult = ParseObjectExtensions.DeepTraversal(obj, true, false);
      Assert.AreEqual(7, traverseResult.Count());

    }

    [Test]
    public void TestRemove() {
      ParseObject obj = ParseObject.Create("Corgi");
      obj["gogo"] = true;
      Assert.True(obj.ContainsKey("gogo"));

      obj.Remove("gogo");
      Assert.False(obj.ContainsKey("gogo"));

      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };

      obj = ParseObjectExtensions.FromState<ParseObject>(state, "Corgi");
      Assert.True(obj.ContainsKey("username"));
      Assert.True(obj.ContainsKey("sessionToken"));

      obj.Remove("username");
      Assert.False(obj.ContainsKey("username"));
      Assert.True(obj.ContainsKey("sessionToken"));
    }

    [Test]
    public void TestIndexGetterSetter() {
      ParseObject obj = ParseObject.Create("Corgi");
      obj["gogo"] = true;
      obj["list"] = new List<string>();
      obj["dict"] = new Dictionary<string, object>();
      obj["fakeACL"] = new ParseACL();
      obj["obj"] = new ParseObject("Corgi");

      Assert.True(obj.ContainsKey("gogo"));
      Assert.IsInstanceOf<bool>(obj["gogo"]);

      Assert.True(obj.ContainsKey("list"));
      Assert.IsInstanceOf<IList<string>>(obj["list"]);

      Assert.True(obj.ContainsKey("dict"));
      Assert.IsInstanceOf<IDictionary<string, object>>(obj["dict"]);

      Assert.True(obj.ContainsKey("fakeACL"));
      Assert.IsInstanceOf<ParseACL>(obj["fakeACL"]);

      Assert.True(obj.ContainsKey("obj"));
      Assert.IsInstanceOf<ParseObject>(obj["obj"]);

      Assert.Throws<KeyNotFoundException>(() => { var gogo = obj["missingItem"]; });
    }

    [Test]
    public void TestPropertiesGetterSetter() {
      var now = new DateTime();
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = now,
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");

      Assert.AreEqual("Pagi", obj.ClassName);
      Assert.AreEqual(now, obj.CreatedAt);
      Assert.Null(obj.UpdatedAt);
      Assert.AreEqual("waGiManPutr4Pet1r", obj.ObjectId);
      Assert.AreEqual(2, obj.Keys.Count());
      Assert.False(obj.IsNew);
      Assert.Null(obj.ACL);
    }

    [Test]
    public void TestAddToList() {
      ParseObject obj = new ParseObject("Corgi");
      obj.AddToList("emptyList", "gogo");
      obj["existingList"] = new List<string>() { "rich" };

      Assert.True(obj.ContainsKey("emptyList"));
      Assert.AreEqual(1, obj.Get<List<object>>("emptyList").Count);

      obj.AddToList("existingList", "gogo");
      Assert.True(obj.ContainsKey("existingList"));
      Assert.AreEqual(2, obj.Get<List<object>>("existingList").Count);

      obj.AddToList("existingList", 1);
      Assert.AreEqual(3, obj.Get<List<object>>("existingList").Count);

      obj.AddRangeToList("newRange", new List<string>() { "anti", "mage" });
      Assert.AreEqual(2, obj.Get<List<object>>("newRange").Count);
    }

    [Test]
    public void TestAddUniqueToList() {
      ParseObject obj = new ParseObject("Corgi");
      obj.AddUniqueToList("emptyList", "gogo");
      obj["existingList"] = new List<string>() { "gogo" };

      Assert.True(obj.ContainsKey("emptyList"));
      Assert.AreEqual(1, obj.Get<List<object>>("emptyList").Count);

      obj.AddUniqueToList("existingList", "gogo");
      Assert.True(obj.ContainsKey("existingList"));
      Assert.AreEqual(1, obj.Get<List<object>>("existingList").Count);

      obj.AddUniqueToList("existingList", 1);
      Assert.AreEqual(2, obj.Get<List<object>>("existingList").Count);

      obj.AddRangeUniqueToList("newRange", new List<string>() { "anti", "anti" });
      Assert.AreEqual(1, obj.Get<List<object>>("newRange").Count);
    }

    [Test]
    public void TestRemoveAllFromList() {
      ParseObject obj = new ParseObject("Corgi");
      obj["existingList"] = new List<string>() { "gogo", "Queen of Pain" };

      obj.RemoveAllFromList("existingList", new List<string>() { "gogo", "missingItem" });
      Assert.AreEqual(1, obj.Get<List<object>>("existingList").Count);
    }

    [Test]
    public void TestGetRelation() {
      // TODO (hallucinogen): do this
    }

    [Test]
    public void TestTryGetValue() {
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");
      string res = null;
      obj.TryGetValue<string>("username", out res);
      Assert.AreEqual("kevin", res);

      ParseObject resObj = null;
      obj.TryGetValue<ParseObject>("username", out resObj);
      Assert.Null(resObj);

      obj.TryGetValue<string>("missingItem", out res);
      Assert.Null(res);
    }

    [Test]
    public void TestGet() {
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");
      Assert.AreEqual("kevin", obj.Get<string>("username"));
      Assert.Throws<InvalidCastException>(() => obj.Get<ParseObject>("username"));
      Assert.Throws<KeyNotFoundException>(() => obj.Get<string>("missingItem"));
    }

    [Test]
    public void TestIsDataAvailable() {
      // TODO (hallucinogen): do this
    }

    [Test]
    public void TestHasSameId() {
      // TODO (hallucinogen): do this
    }

    [Test]
    public void TestKeys() {
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");
      Assert.AreEqual(2, obj.Keys.Count);

      obj["additional"] = true;
      Assert.AreEqual(3, obj.Keys.Count);

      obj.Remove("username");
      Assert.AreEqual(2, obj.Keys.Count);
    }

    [Test]
    public void TestAdd() {
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");
      Assert.Throws<ArgumentException>(() => obj.Add("username", "kevin"));

      obj.Add("zeus", "bewithyou");
      Assert.AreEqual("bewithyou", obj["zeus"]);
    }

    [Test]
    public void TestEnumerator() {
      IObjectState state = new MutableObjectState {
        ObjectId = "waGiManPutr4Pet1r",
        ClassName = "Pagi",
        CreatedAt = new DateTime(),
        ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
      };
      ParseObject obj = ParseObjectExtensions.FromState<ParseObject>(state, "Omitted");

      int count = 0;
      foreach (var key in obj) {
        count++;
      }
      Assert.AreEqual(2, count);

      obj["newDirtyItem"] = "newItem";
      count = 0;
      foreach (var key in obj) {
        count++;
      }
      Assert.AreEqual(3, count);
    }

    [Test]
    public void TestGetQuery() {
      ParseObject.RegisterSubclass<SubClass>();

      ParseQuery<ParseObject> query = ParseObject.GetQuery("UnregisteredSubClass");
      Assert.AreEqual("UnregisteredSubClass", query.GetClassName());

      Assert.Throws<ArgumentException>(() => ParseObject.GetQuery("SubClass"));

      ParseCorePlugins.Instance.SubclassingController.UnregisterSubclass(typeof(SubClass));
    }

    [Test]
    public void TestPropertyChanged() {
      // TODO (hallucinogen): do this
    }

    [Test]
    [AsyncStateMachine(typeof(ObjectTests))]
    public Task TestSave() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(ObjectTests))]
    public Task TestSaveAll() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(ObjectTests))]
    public Task TestDelete() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(ObjectTests))]
    public Task TestDeleteAll() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(ObjectTests))]
    public Task TestFetch() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }

    [Test]
    [AsyncStateMachine(typeof(ObjectTests))]
    public Task TestFetchAll() {
      // TODO (hallucinogen): do this
      return Task.FromResult(0);
    }
  }
}
