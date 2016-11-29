using Moq;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime;

namespace ParseTest
{
    [TestFixture]
    public class ObjectTests
    {
        [AVClassName("SubClass")]
        private class SubClass : AVObject
        {
        }

        [AVClassName("UnregisteredSubClass")]
        private class UnregisteredSubClass : AVObject
        {
        }

        [TearDown]
        public void TearDown()
        {
            AVPlugins.Instance.Reset();
        }

        [Test]
        public void TestParseObjectConstructor()
        {
            AVObject obj = new AVObject("Corgi");
            Assert.AreEqual("Corgi", obj.ClassName);
            Assert.Null(obj.CreatedAt);
            Assert.True(obj.IsDataAvailable);
            Assert.True(obj.IsDirty);
        }

        [Test]
        public void TestParseObjectCreate()
        {
            AVObject obj = AVObject.Create("Corgi");
            Assert.AreEqual("Corgi", obj.ClassName);
            Assert.Null(obj.CreatedAt);
            Assert.True(obj.IsDataAvailable);
            Assert.True(obj.IsDirty);

            AVObject obj2 = AVObject.CreateWithoutData("Corgi", "waGiManPutr4Pet1r");
            Assert.AreEqual("Corgi", obj2.ClassName);
            Assert.AreEqual("waGiManPutr4Pet1r", obj2.ObjectId);
            Assert.Null(obj2.CreatedAt);
            Assert.False(obj2.IsDataAvailable);
            Assert.False(obj2.IsDirty);
        }

        [Test]
        public void TestParseObjectCreateWithGeneric()
        {
            AVObject.RegisterSubclass<SubClass>();

            AVObject obj = AVObject.Create<SubClass>();
            Assert.AreEqual("SubClass", obj.ClassName);
            Assert.Null(obj.CreatedAt);
            Assert.True(obj.IsDataAvailable);
            Assert.True(obj.IsDirty);

            AVObject obj2 = AVObject.CreateWithoutData<SubClass>("waGiManPutr4Pet1r");
            Assert.AreEqual("SubClass", obj2.ClassName);
            Assert.AreEqual("waGiManPutr4Pet1r", obj2.ObjectId);
            Assert.Null(obj2.CreatedAt);
            Assert.False(obj2.IsDataAvailable);
            Assert.False(obj2.IsDirty);
        }

        [Test]
        public void TestParseObjectCreateWithGenericFailWithoutSubclass()
        {
            Assert.Throws<InvalidCastException>(() => AVObject.Create<SubClass>());
        }

        [Test]
        public void TestFromState()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");

            Assert.AreEqual("waGiManPutr4Pet1r", obj.ObjectId);
            Assert.AreEqual("Pagi", obj.ClassName);
            Assert.NotNull(obj.CreatedAt);
            Assert.Null(obj.UpdatedAt);
            Assert.AreEqual("kevin", obj["username"]);
            Assert.AreEqual("se551onT0k3n", obj["sessionToken"]);
        }

        [Test]
        public void TestRegisterSubclass()
        {
            Assert.Throws<InvalidCastException>(() => AVObject.Create<SubClass>());

            AVObject.RegisterSubclass<SubClass>();
            Assert.DoesNotThrow(() => AVObject.Create<SubClass>());

            AVPlugins.Instance.SubclassingController.UnregisterSubclass(typeof(UnregisteredSubClass));
            Assert.DoesNotThrow(() => AVObject.Create<SubClass>());

            AVPlugins.Instance.SubclassingController.UnregisterSubclass(typeof(SubClass));
            Assert.Throws<InvalidCastException>(() => AVObject.Create<SubClass>());
        }

        [Test]
        public void TestRevert()
        {
            AVObject obj = AVObject.Create("Corgi");
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
        public void TestDeepTraversal()
        {
            AVObject obj = AVObject.Create("Corgi");
            IDictionary<string, object> someDict = new Dictionary<string, object>() {
        { "someList", new List<object>() }
      };
            obj["obj"] = AVObject.Create("Pug");
            obj["obj2"] = AVObject.Create("Pug");
            obj["list"] = new List<object>();
            obj["dict"] = someDict;
            obj["someBool"] = true;
            obj["someInt"] = 23;

            IEnumerable<object> traverseResult = AVObjectExtensions.DeepTraversal(obj, true, true);
            Assert.AreEqual(8, traverseResult.Count());

            // Don't traverse beyond the root (since root is AVObject)
            traverseResult = AVObjectExtensions.DeepTraversal(obj, false, true);
            Assert.AreEqual(1, traverseResult.Count());

            traverseResult = AVObjectExtensions.DeepTraversal(someDict, false, true);
            Assert.AreEqual(2, traverseResult.Count());

            // Should ignore root
            traverseResult = AVObjectExtensions.DeepTraversal(obj, true, false);
            Assert.AreEqual(7, traverseResult.Count());

        }

        [Test]
        public void TestRemove()
        {
            AVObject obj = AVObject.Create("Corgi");
            obj["gogo"] = true;
            Assert.True(obj.ContainsKey("gogo"));

            obj.Remove("gogo");
            Assert.False(obj.ContainsKey("gogo"));

            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };

            obj = AVObjectExtensions.FromState<AVObject>(state, "Corgi");
            Assert.True(obj.ContainsKey("username"));
            Assert.True(obj.ContainsKey("sessionToken"));

            obj.Remove("username");
            Assert.False(obj.ContainsKey("username"));
            Assert.True(obj.ContainsKey("sessionToken"));
        }

        [Test]
        public void TestIndexGetterSetter()
        {
            AVObject obj = AVObject.Create("Corgi");
            obj["gogo"] = true;
            obj["list"] = new List<string>();
            obj["dict"] = new Dictionary<string, object>();
            obj["fakeACL"] = new AVACL();
            obj["obj"] = new AVObject("Corgi");

            Assert.True(obj.ContainsKey("gogo"));
            Assert.IsInstanceOf<bool>(obj["gogo"]);

            Assert.True(obj.ContainsKey("list"));
            Assert.IsInstanceOf<IList<string>>(obj["list"]);

            Assert.True(obj.ContainsKey("dict"));
            Assert.IsInstanceOf<IDictionary<string, object>>(obj["dict"]);

            Assert.True(obj.ContainsKey("fakeACL"));
            Assert.IsInstanceOf<AVACL>(obj["fakeACL"]);

            Assert.True(obj.ContainsKey("obj"));
            Assert.IsInstanceOf<AVObject>(obj["obj"]);

            Assert.Throws<KeyNotFoundException>(() => { var gogo = obj["missingItem"]; });
        }

        [Test]
        public void TestPropertiesGetterSetter()
        {
            var now = new DateTime();
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = now,
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");

            Assert.AreEqual("Pagi", obj.ClassName);
            Assert.AreEqual(now, obj.CreatedAt);
            Assert.Null(obj.UpdatedAt);
            Assert.AreEqual("waGiManPutr4Pet1r", obj.ObjectId);
            Assert.AreEqual(2, obj.Keys.Count());
            Assert.False(obj.IsNew);
            Assert.Null(obj.ACL);
        }

        [Test]
        public void TestAddToList()
        {
            AVObject obj = new AVObject("Corgi");
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
        public void TestAddUniqueToList()
        {
            AVObject obj = new AVObject("Corgi");
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
        public void TestRemoveAllFromList()
        {
            AVObject obj = new AVObject("Corgi");
            obj["existingList"] = new List<string>() { "gogo", "Queen of Pain" };

            obj.RemoveAllFromList("existingList", new List<string>() { "gogo", "missingItem" });
            Assert.AreEqual(1, obj.Get<List<object>>("existingList").Count);
        }

        [Test]
        public void TestGetRelation()
        {
            // TODO (hallucinogen): do this
        }

        [Test]
        public void TestTryGetValue()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");
            string res = null;
            obj.TryGetValue<string>("username", out res);
            Assert.AreEqual("kevin", res);

            AVObject resObj = null;
            obj.TryGetValue<AVObject>("username", out resObj);
            Assert.Null(resObj);

            obj.TryGetValue<string>("missingItem", out res);
            Assert.Null(res);
        }

        [Test]
        public void TestGet()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");
            Assert.AreEqual("kevin", obj.Get<string>("username"));
            Assert.Throws<InvalidCastException>(() => obj.Get<AVObject>("username"));
            Assert.Throws<KeyNotFoundException>(() => obj.Get<string>("missingItem"));
        }

        [Test]
        public void TestIsDataAvailable()
        {
            // TODO (hallucinogen): do this
        }

        [Test]
        public void TestHasSameId()
        {
            // TODO (hallucinogen): do this
        }

        [Test]
        public void TestKeys()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");
            Assert.AreEqual(2, obj.Keys.Count);

            obj["additional"] = true;
            Assert.AreEqual(3, obj.Keys.Count);

            obj.Remove("username");
            Assert.AreEqual(2, obj.Keys.Count);
        }

        [Test]
        public void TestAdd()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");
            Assert.Throws<ArgumentException>(() => obj.Add("username", "kevin"));

            obj.Add("zeus", "bewithyou");
            Assert.AreEqual("bewithyou", obj["zeus"]);
        }

        [Test]
        public void TestEnumerator()
        {
            IObjectState state = new MutableObjectState
            {
                ObjectId = "waGiManPutr4Pet1r",
                ClassName = "Pagi",
                CreatedAt = new DateTime(),
                ServerData = new Dictionary<string, object>() {
          { "username", "kevin" },
          { "sessionToken", "se551onT0k3n" }
        }
            };
            AVObject obj = AVObjectExtensions.FromState<AVObject>(state, "Omitted");

            int count = 0;
            foreach (var key in obj)
            {
                count++;
            }
            Assert.AreEqual(2, count);

            obj["newDirtyItem"] = "newItem";
            count = 0;
            foreach (var key in obj)
            {
                count++;
            }
            Assert.AreEqual(3, count);
        }

        [Test]
        public void TestGetQuery()
        {
            AVObject.RegisterSubclass<SubClass>();

            AVQuery<AVObject> query = AVObject.GetQuery("UnregisteredSubClass");
            Assert.AreEqual("UnregisteredSubClass", query.GetClassName());

            Assert.Throws<ArgumentException>(() => AVObject.GetQuery("SubClass"));

            AVPlugins.Instance.SubclassingController.UnregisterSubclass(typeof(SubClass));
        }

        [Test]
        public void TestPropertyChanged()
        {
            // TODO (hallucinogen): do this
        }

        [Test]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestSave()
        {
            //AVClient.Initialize("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
            //var avObject = new AVObject("TestObject");
            //avObject["key"] = "value";
            //return avObject.SaveAsync().ContinueWith(t =>
            // {
            //     Console.WriteLine(avObject.ObjectId);
            //     return Task.FromResult(0);
            // }).Unwrap();
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
            return realtime.CreateClient("junwu").ContinueWith(t =>
            {
                var client = t.Result;
                Console.WriteLine(client.State.ToString());
                return Task.FromResult(0);
            }).Unwrap();

        }

        [Test]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestSaveAll()
        {
            // TODO (hallucinogen): do this
            return Task.FromResult(0);
        }

        [Test]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestDelete()
        {
            // TODO (hallucinogen): do this
            return Task.FromResult(0);
        }

        [Test]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestDeleteAll()
        {
            // TODO (hallucinogen): do this
            return Task.FromResult(0);
        }

        [Test]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestFetch()
        {
            // TODO (hallucinogen): do this
            return Task.FromResult(0);
        }

        [Test]
        [AsyncStateMachine(typeof(ObjectTests))]
        public Task TestFetchAll()
        {
            // TODO (hallucinogen): do this
            return Task.FromResult(0);
        }
    }
}
