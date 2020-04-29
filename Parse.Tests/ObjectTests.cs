using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Internal;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests
{
    [TestClass]
    public class ObjectTests
    {
        [ParseClassName(nameof(SubClass))]
        class SubClass : ParseObject { }

        [ParseClassName(nameof(UnregisteredSubClass))]
        class UnregisteredSubClass : ParseObject { }

        ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

        [TestCleanup]
        public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestParseObjectConstructor()
        {
            ParseObject obj = new ParseObject("Corgi");
            Assert.AreEqual("Corgi", obj.ClassName);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsTrue(obj.IsDataAvailable);
            Assert.IsTrue(obj.IsDirty);
        }

        [TestMethod]
        public void TestParseObjectCreate()
        {
            ParseObject obj = Client.CreateObject("Corgi");
            Assert.AreEqual("Corgi", obj.ClassName);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsTrue(obj.IsDataAvailable);
            Assert.IsTrue(obj.IsDirty);

            ParseObject obj2 = Client.CreateObjectWithoutData("Corgi", "waGiManPutr4Pet1r");
            Assert.AreEqual("Corgi", obj2.ClassName);
            Assert.AreEqual("waGiManPutr4Pet1r", obj2.ObjectId);
            Assert.IsNull(obj2.CreatedAt);
            Assert.IsFalse(obj2.IsDataAvailable);
            Assert.IsFalse(obj2.IsDirty);
        }

        [TestMethod]
        public void TestParseObjectCreateWithGeneric()
        {
            Client.AddValidClass<SubClass>();

            ParseObject obj = Client.CreateObject<SubClass>();
            Assert.AreEqual(nameof(SubClass), obj.ClassName);
            Assert.IsNull(obj.CreatedAt);
            Assert.IsTrue(obj.IsDataAvailable);
            Assert.IsTrue(obj.IsDirty);

            ParseObject obj2 = Client.CreateObjectWithoutData<SubClass>("waGiManPutr4Pet1r");
            Assert.AreEqual(nameof(SubClass), obj2.ClassName);
            Assert.AreEqual("waGiManPutr4Pet1r", obj2.ObjectId);
            Assert.IsNull(obj2.CreatedAt);
            Assert.IsFalse(obj2.IsDataAvailable);
            Assert.IsFalse(obj2.IsDirty);
        }

        [TestMethod]
        public void TestParseObjectCreateWithGenericFailWithoutSubclass() => Assert.ThrowsException<InvalidCastException>(() => Client.CreateObject<SubClass>());

        [TestMethod]
        public void TestFromState()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");

            Assert.AreEqual("waGiManPutr4Pet1r", obj.ObjectId);
            Assert.AreEqual("Pagi", obj.ClassName);
            Assert.IsNotNull(obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.AreEqual("kevin", obj["username"]);
            Assert.AreEqual("se551onT0k3n", obj["sessionToken"]);
        }

        [TestMethod]
        public void TestRegisterSubclass()
        {
            Assert.ThrowsException<InvalidCastException>(() => Client.CreateObject<SubClass>());

            try
            {
                Client.AddValidClass<SubClass>();
                Client.CreateObject<SubClass>();

                Client.ClassController.RemoveClass(typeof(UnregisteredSubClass));
                Client.CreateObject<SubClass>();
            }
            catch { Assert.Fail(); }

            Client.ClassController.RemoveClass(typeof(SubClass));
            Assert.ThrowsException<InvalidCastException>(() => Client.CreateObject<SubClass>());
        }

        [TestMethod]
        public void TestRevert()
        {
            ParseObject obj = Client.CreateObject("Corgi");
            obj["gogo"] = true;

            Assert.IsTrue(obj.IsDirty);
            Assert.AreEqual(1, obj.CurrentOperations.Count);
            Assert.IsTrue(obj.ContainsKey("gogo"));

            obj.Revert();

            Assert.IsTrue(obj.IsDirty);
            Assert.AreEqual(0, obj.CurrentOperations.Count);
            Assert.IsFalse(obj.ContainsKey("gogo"));
        }

        [TestMethod]
        public void TestDeepTraversal()
        {
            ParseObject obj = Client.CreateObject("Corgi");

            IDictionary<string, object> someDict = new Dictionary<string, object>
            {
                ["someList"] = new List<object> { }
            };

            obj[nameof(obj)] = Client.CreateObject("Pug");
            obj["obj2"] = Client.CreateObject("Pug");
            obj["list"] = new List<object>();
            obj["dict"] = someDict;
            obj["someBool"] = true;
            obj["someInt"] = 23;

            IEnumerable<object> traverseResult = Client.TraverseObjectDeep(obj, true, true);
            Assert.AreEqual(8, traverseResult.Count());

            // Don't traverse beyond the root (since root is ParseObject).

            traverseResult = Client.TraverseObjectDeep(obj, false, true);
            Assert.AreEqual(1, traverseResult.Count());

            traverseResult = Client.TraverseObjectDeep(someDict, false, true);
            Assert.AreEqual(2, traverseResult.Count());

            // Should ignore root.

            traverseResult = Client.TraverseObjectDeep(obj, true, false);
            Assert.AreEqual(7, traverseResult.Count());

        }

        [TestMethod]
        public void TestRemove()
        {
            ParseObject obj = Client.CreateObject("Corgi");
            obj["gogo"] = true;
            Assert.IsTrue(obj.ContainsKey("gogo"));

            obj.Remove("gogo");
            Assert.IsFalse(obj.ContainsKey("gogo"));

            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            obj = Client.GenerateObjectFromState<ParseObject>(state, "Corgi");
            Assert.IsTrue(obj.ContainsKey("username"));
            Assert.IsTrue(obj.ContainsKey("sessionToken"));

            obj.Remove("username");
            Assert.IsFalse(obj.ContainsKey("username"));
            Assert.IsTrue(obj.ContainsKey("sessionToken"));
        }

        [TestMethod]
        public void TestIndexGetterSetter()
        {
            ParseObject obj = Client.CreateObject("Corgi");
            obj["gogo"] = true;
            obj["list"] = new List<string>();
            obj["dict"] = new Dictionary<string, object>();
            obj["fakeACL"] = new ParseACL();
            obj[nameof(obj)] = new ParseObject("Corgi");

            Assert.IsTrue(obj.ContainsKey("gogo"));
            Assert.IsInstanceOfType(obj["gogo"], typeof(bool));

            Assert.IsTrue(obj.ContainsKey("list"));
            Assert.IsInstanceOfType(obj["list"], typeof(IList<string>));

            Assert.IsTrue(obj.ContainsKey("dict"));
            Assert.IsInstanceOfType(obj["dict"], typeof(IDictionary<string, object>));

            Assert.IsTrue(obj.ContainsKey("fakeACL"));
            Assert.IsInstanceOfType(obj["fakeACL"], typeof(ParseACL));

            Assert.IsTrue(obj.ContainsKey(nameof(obj)));
            Assert.IsInstanceOfType(obj[nameof(obj)], typeof(ParseObject));

            Assert.ThrowsException<KeyNotFoundException>(() => { object gogo = obj["missingItem"]; });
        }

        [TestMethod]
        public void TestPropertiesGetterSetter()
        {
            DateTime now = new DateTime { };

            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = now,
                ServerData = new Dictionary<string, object>
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");

            Assert.AreEqual("Pagi", obj.ClassName);
            Assert.AreEqual(now, obj.CreatedAt);
            Assert.IsNull(obj.UpdatedAt);
            Assert.AreEqual("waGiManPutr4Pet1r", obj.ObjectId);
            Assert.AreEqual(2, obj.Keys.Count());
            Assert.IsFalse(obj.IsNew);
            Assert.IsNull(obj.ACL);
        }

        [TestMethod]
        public void TestAddToList()
        {
            ParseObject obj = new ParseObject("Corgi").Bind(Client);

            obj.AddToList("emptyList", "gogo");
            obj["existingList"] = new List<string>() { "rich" };

            Assert.IsTrue(obj.ContainsKey("emptyList"));
            Assert.AreEqual(1, obj.Get<List<object>>("emptyList").Count);

            obj.AddToList("existingList", "gogo");
            Assert.IsTrue(obj.ContainsKey("existingList"));
            Assert.AreEqual(2, obj.Get<List<object>>("existingList").Count);

            obj.AddToList("existingList", 1);
            Assert.AreEqual(3, obj.Get<List<object>>("existingList").Count);

            obj.AddRangeToList("newRange", new List<string>() { "anti", "mage" });
            Assert.AreEqual(2, obj.Get<List<object>>("newRange").Count);
        }

        [TestMethod]
        public void TestAddUniqueToList()
        {
            ParseObject obj = new ParseObject("Corgi").Bind(Client);

            obj.AddUniqueToList("emptyList", "gogo");
            obj["existingList"] = new List<string>() { "gogo" };

            Assert.IsTrue(obj.ContainsKey("emptyList"));
            Assert.AreEqual(1, obj.Get<List<object>>("emptyList").Count);

            obj.AddUniqueToList("existingList", "gogo");
            Assert.IsTrue(obj.ContainsKey("existingList"));
            Assert.AreEqual(1, obj.Get<List<object>>("existingList").Count);

            obj.AddUniqueToList("existingList", 1);
            Assert.AreEqual(2, obj.Get<List<object>>("existingList").Count);

            obj.AddRangeUniqueToList("newRange", new List<string>() { "anti", "anti" });
            Assert.AreEqual(1, obj.Get<List<object>>("newRange").Count);
        }

        [TestMethod]
        public void TestRemoveAllFromList()
        {
            ParseObject obj = new ParseObject("Corgi", Client) { ["existingList"] = new List<string> { "gogo", "Queen of Pain" } };

            obj.RemoveAllFromList("existingList", new List<string>() { "gogo", "missingItem" });
            Assert.AreEqual(1, obj.Get<List<object>>("existingList").Count);
        }

        [TestMethod]
        public void TestGetRelation()
        {
            // TODO (hallucinogen): do this
        }

        [TestMethod]
        public void TestTryGetValue()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>()
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");

            obj.TryGetValue("username", out string res);
            Assert.AreEqual("kevin", res);

            obj.TryGetValue("username", out ParseObject resObj);
            Assert.IsNull(resObj);

            obj.TryGetValue("missingItem", out res);
            Assert.IsNull(res);
        }

        [TestMethod]
        public void TestGet()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>()
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };

            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");
            Assert.AreEqual("kevin", obj.Get<string>("username"));
            Assert.ThrowsException<InvalidCastException>(() => obj.Get<ParseObject>("username"));
            Assert.ThrowsException<KeyNotFoundException>(() => obj.Get<string>("missingItem"));
        }

#warning Some tests are not implemented.

        [TestMethod]
        public void TestIsDataAvailable()
        {
            // TODO (hallucinogen): do this
        }

        [TestMethod]
        public void TestHasSameId()
        {
            // TODO (hallucinogen): do this
        }

        [TestMethod]
        public void TestKeys()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>()
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };
            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");
            Assert.AreEqual(2, obj.Keys.Count);

            obj["additional"] = true;
            Assert.AreEqual(3, obj.Keys.Count);

            obj.Remove("username");
            Assert.AreEqual(2, obj.Keys.Count);
        }

        [TestMethod]
        public void TestAdd()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>()
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };
            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");
            Assert.ThrowsException<ArgumentException>(() => obj.Add("username", "kevin"));

            obj.Add("zeus", "bewithyou");
            Assert.AreEqual("bewithyou", obj["zeus"]);
        }

        [TestMethod]
        public void TestEnumerator()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime { },
                ServerData = new Dictionary<string, object>()
                {
                    ["username"] = "kevin",
                    ["sessionToken"] = "se551onT0k3n"
                }
            };
            ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Omitted");

            int count = 0;

            foreach (KeyValuePair<string, object> key in obj)
            {
                count++;
            }

            Assert.AreEqual(2, count);

            obj["newDirtyItem"] = "newItem";
            count = 0;

            foreach (KeyValuePair<string, object> key in obj)
            {
                count++;
            }

            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public void TestGetQuery()
        {
            Client.AddValidClass<SubClass>();

            ParseQuery<ParseObject> query = Client.GetQuery(nameof(UnregisteredSubClass));
            Assert.AreEqual(nameof(UnregisteredSubClass), query.GetClassName());

            Assert.ThrowsException<ArgumentException>(() => Client.GetQuery(nameof(SubClass)));

            Client.ClassController.RemoveClass(typeof(SubClass));
        }

#warning These tests are incomplete.

        [TestMethod]
        public void TestPropertyChanged()
        {
            // TODO (hallucinogen): do this
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestSave() =>
            // TODO (hallucinogen): do this
            Task.FromResult(0);

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestSaveAll() =>
            // TODO (hallucinogen): do this
            Task.FromResult(0);

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestDelete() =>
            // TODO (hallucinogen): do this
            Task.FromResult(0);

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestDeleteAll() =>
            // TODO (hallucinogen): do this
            Task.FromResult(0);

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestFetch() =>
            // TODO (hallucinogen): do this
            Task.FromResult(0);

        [TestMethod]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestFetchAll() =>
            // TODO (hallucinogen): do this
            Task.FromResult(0);
    }
}
