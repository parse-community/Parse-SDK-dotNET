using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Internal;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class ObjectTests
{
    [ParseClassName(nameof(SubClass))]
    class SubClass : ParseObject { }

    [ParseClassName(nameof(UnregisteredSubClass))]
    class UnregisteredSubClass : ParseObject { }
        private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize the client and ensure the instance is set
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();
        // Register the valid classes
        Client.AddValidClass<ParseSession>();
        Client.AddValidClass<ParseUser>();
    }
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
        obj[nameof(obj)] = new ParseObject("Corgi", Client);

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

        Assert.IsNull(obj["missingItem"]);
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

#warning Some tests are not implemented.

    [TestMethod]
    public void TestIsDataAvailable()
    {
        var obj1 = Client.CreateObject("TestClass");
        Assert.IsTrue(obj1.IsDataAvailable);

        var obj2 = Client.CreateObjectWithoutData("TestClass", "objectId");
        Assert.IsFalse(obj2.IsDataAvailable);
    }

    [TestMethod]
    public void TestHasSameId()
    {
        var obj1 = Client.CreateObject("TestClass");
        obj1.ObjectId = "testId";

        var obj2 = Client.CreateObjectWithoutData("TestClass", "testId");
        Assert.IsTrue(obj1.HasSameId(obj2));

        var obj3 = Client.CreateObjectWithoutData("TestClass", "differentId");
        Assert.IsFalse(obj1.HasSameId(obj3));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException), "You can't add an unsaved ParseObject to a relation.")]
    public void TestGetRelation_UnsavedObject()
    {
        var parentObj = Client.CreateObject("ParentClass");
        var childObj = Client.CreateObject("ChildClass");

        var relation = parentObj.GetRelation<ParseObject>("childRelation");
        relation.Add(childObj); // Should throw an exception
    }

    [TestMethod]
    public void TestGetRelation_SavedObject()
    {
        //Todo : (YB) I will leave this to anyone else!
    }



    [TestMethod]
    public void TestPropertyChanged()
    {
        var obj = Client.CreateObject("TestClass");
        bool propertyChangedFired = false;

        var eventRaised = new ManualResetEvent(false);

        obj.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "key")
            {
                propertyChangedFired = true;
                eventRaised.Set();  // Signal that the event has been raised
            }
        };

        obj["key"] = "value";

        // Wait for the event to be raised. Not necessary in production code.
        eventRaised.WaitOne();

        Assert.IsTrue(propertyChangedFired);
    }


    [TestMethod]
    public async Task TestSave()
    {
        var mockController = new Mock<IParseObjectController>();

        // Modify the mock to simulate a server response with ObjectId set after save
        mockController.Setup(ctrl =>
                ctrl.SaveAsync(It.IsAny<IObjectState>(), It.IsAny<IDictionary<string, IParseFieldOperation>>(), null, It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IObjectState state, IDictionary<string, IParseFieldOperation> operations, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken) =>
            {
                var newState = state as MutableObjectState;
                if (newState != null)
                {
                    // Simulating the server's response after saving the object
                    newState.ObjectId = "savedId"; // This should be the value returned by the server
                }
                return newState;  // Return the updated state with ObjectId set
            });

        var hub = new MutableServiceHub { ObjectController = mockController.Object };
        var client = new ParseClient(new ServerConnectionData { Test = true }, hub);

        var obj = client.CreateObject("TestClass");
        obj["key"] = "value";

        // Save the object
        await obj.SaveAsync();

        // Assert that the ObjectId is set to the expected value returned from the server
        Assert.AreEqual("savedId", obj.ObjectId); // Assert the ObjectId was set correctly
        Assert.IsFalse(obj.IsDirty); // Ensure the object is no longer dirty after save
        mockController.VerifyAll(); // Verify the mock behavior
    }


    [TestMethod]
    public async Task TestSaveAll()
    {
        var mockController = new Mock<IParseObjectController>();

        // Mock SaveAsync for single-object saves
        mockController.Setup(ctrl =>
            ctrl.SaveAsync(It.IsAny<IObjectState>(),
                           It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                           It.IsAny<string>(),
                           It.IsAny<IServiceHub>(),
                           It.IsAny<CancellationToken>()))
        .ReturnsAsync((IObjectState state,
                       IDictionary<string, IParseFieldOperation> operations,
                       string sessionToken,
                       IServiceHub serviceHub,
                       CancellationToken cancellationToken) =>
        {
            // Return updated state with ObjectId
            return new MutableObjectState
            {
                ClassName = state.ClassName,
                ObjectId = $"id-{state.ClassName}" // Generate unique ObjectId
            };
        });

        // Assign the mocked controller to the client
        var client = new ParseClient(new ServerConnectionData { Test = true },
                                      new MutableServiceHub { ObjectController = mockController.Object });

        // Create objects
        var obj1 = client.CreateObject("TestClass1");
        var obj2 = client.CreateObject("TestClass2");

        // Save the objects individually
        await Task.WhenAll(obj1.SaveAsync(), obj2.SaveAsync());

        // Verify the objects have the correct IDs
        Assert.AreEqual("id-TestClass1", obj1.ObjectId); // Check obj1 ID
        Assert.AreEqual("id-TestClass2", obj2.ObjectId); // Check obj2 ID

        // Verify SaveAsync was called for each object
        mockController.Verify(ctrl =>
            ctrl.SaveAsync(It.IsAny<IObjectState>(),
                           It.IsAny<IDictionary<string, IParseFieldOperation>>(),
                           It.IsAny<string>(),
                           It.IsAny<IServiceHub>(),
                           It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Ensure it was called twice, once for each object
    }



    [TestMethod]
    public async Task TestDelete()
    {
        // Mock the object controller
        var mockController = new Mock<IParseObjectController>();
        mockController
            .Setup(ctrl =>
                ctrl.DeleteAsync(It.IsAny<IObjectState>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Create a ParseClient with the mocked controller
        var serviceHub = new MutableServiceHub { ObjectController = mockController.Object };
        var client = new ParseClient(new ServerConnectionData { Test = true }, serviceHub);

        // Create a ParseObject and assign an ObjectId
        var obj = client.CreateObject("TestClass");
        obj.ObjectId = "toDelete";

        // Perform the delete operation
        await obj.DeleteAsync();

        // Verify the DeleteAsync method was called on the controller
        mockController.Verify(ctrl =>
            ctrl.DeleteAsync(It.Is<IObjectState>(state => state.ObjectId == "toDelete"), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task TestDeleteAll_WithDeleteAsync()
    {
        // Mock the object controller
        var mockController = new Mock<IParseObjectController>();

        // Mock DeleteAsync for individual object deletes
        mockController
            .Setup(ctrl =>
                ctrl.DeleteAsync(It.IsAny<IObjectState>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Create a ParseClient with the mocked controller
        var serviceHub = new MutableServiceHub { ObjectController = mockController.Object };
        var client = new ParseClient(new ServerConnectionData { Test = true }, serviceHub);

        // Create ParseObjects and assign ObjectIds
        var obj1 = client.CreateObject("TestClass1");
        var obj2 = client.CreateObject("TestClass2");

        obj1.ObjectId = "toDelete1";
        obj2.ObjectId = "toDelete2";

        // Perform delete operations
        await Task.WhenAll(obj1.DeleteAsync(), obj2.DeleteAsync());

        // Verify DeleteAsync was called for each object
        mockController.Verify(ctrl =>
            ctrl.DeleteAsync(It.Is<IObjectState>(state => state.ObjectId == "toDelete1"), null, It.IsAny<CancellationToken>()),
            Times.Once);

        mockController.Verify(ctrl =>
            ctrl.DeleteAsync(It.Is<IObjectState>(state => state.ObjectId == "toDelete2"), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [TestMethod]
    public async Task TestFetch()
    {
        // Arrange
        var mockController = new Mock<IParseObjectController>();
        mockController.Setup(ctrl =>
            ctrl.FetchAsync(It.IsAny<IObjectState>(), null, It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MutableObjectState
            {
                ObjectId = "fetchedId",
                ServerData = new Dictionary<string, object> { ["key"] = "value" }
            });

        var serviceHub = new MutableServiceHub
        {
            ObjectController = mockController.Object
        };
        var client = new ParseClient(new ServerConnectionData { Test = true }, serviceHub);

        // Act
        var obj = client.CreateObjectWithoutData("TestClass", "fetchedId");
        await obj.FetchAsync();

        // Assert
        Assert.AreEqual("value", obj["key"]);
        mockController.Verify(ctrl =>
            ctrl.FetchAsync(It.Is<IObjectState>(state => state.ObjectId == "fetchedId"), null, It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [TestMethod]
    public void TestFetchAll()
    {
        
    }
}
