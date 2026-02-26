using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;

namespace Parse.Tests;

[TestClass]
public class ObjectServiceExtensionsTests
{

    // simple subclass for testing instance creation
    [ParseClassName(nameof(TestObject))]
    class TestObject : ParseObject { }

    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize the client and ensure the instance is set
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();
    }

    [TestMethod]
    public void AddValidClass_InvokesController()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        mockHub.Object.AddValidClass<TestObject>();

        mockController.Verify(c => c.AddValid(typeof(TestObject)), Times.Once);
    }

    [TestMethod]
    public void RegisterSubclass_DoesNothingForNonParseObject()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        mockHub.Object.RegisterSubclass(typeof(string));

        mockController.Verify(c => c.AddValid(It.IsAny<Type>()), Times.Never);
    }

    [TestMethod]
    public void RegisterSubclass_CallsAddValidForParseObject()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        mockHub.Object.RegisterSubclass(typeof(TestObject));

        mockController.Verify(c => c.AddValid(typeof(TestObject)), Times.Once);
    }

    [TestMethod]
    public void RemoveClass_Generic_InvokesController()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        mockHub.Object.RemoveClass<TestObject>();
        mockController.Verify(c => c.RemoveClass(typeof(TestObject)), Times.Once);
    }

    [TestMethod]
    public void RemoveClass_ControllerExtension_NonParseObject_NoCall()
    {
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        // call the static extension explicitly so the check is executed
        ObjectServiceExtensions.RemoveClass(mockController.Object, typeof(string));
        mockController.Verify(c => c.RemoveClass(It.IsAny<Type>()), Times.Never);
    }

    [TestMethod]
    public void RemoveClass_ControllerExtension_ParseObject_CallsRemove()
    {
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        ObjectServiceExtensions.RemoveClass(mockController.Object, typeof(TestObject));
        mockController.Verify(c => c.RemoveClass(typeof(TestObject)), Times.Once);
    }

    [TestMethod]
    public void CreateObject_ReturnsInstance()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();

        Client.AddValidClass<TestObject>();
        TestObject expected = Client.CreateObject<TestObject>();

        mockController.Setup(c => c.Instantiate("TestObject", mockHub.Object)).Returns(expected);
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        ParseObject result = mockHub.Object.CreateObject("TestObject");
        Assert.AreSame(expected, result);
    }

    [TestMethod]
    public void CreateObjectWithoutData_CreatesPointer()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();

        Client.AddValidClass<TestObject>();
        TestObject obj = Client.CreateObject<TestObject>();

        mockController.Setup(c => c.Instantiate("TestObject", mockHub.Object)).Returns(obj);
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        ParseObject pointer = mockHub.Object.CreateObjectWithoutData("TestObject", "abc123");
        Assert.AreSame(obj, pointer);
        Assert.AreEqual("abc123", pointer.ObjectId);
        Assert.IsFalse(pointer.IsDirty, "pointer should not be marked dirty");
        Assert.IsFalse(ParseObject.CreatingPointer.Value, "thread local flag should be reset");
    }

    [TestMethod]
    public void CreateObjectWithData_NullThrows()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Assert.ThrowsException<ArgumentNullException>(() => mockHub.Object.CreateObjectWithData<TestObject>(null));
    }

    [TestMethod]
    public void GetQuery_ThrowsWhenClassExists()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockController.Setup(c => c.GetType("TestObject")).Returns(() => typeof(TestObject));
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        Assert.ThrowsException<ArgumentException>(() => mockHub.Object.GetQuery("TestObject"));
    }

    [TestMethod]
    public void GetQuery_ReturnsNewQuery()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockController.Setup(c => c.GetType(It.IsAny<string>())).Returns(() => null as Type);
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);

        ParseQuery<ParseObject> query = mockHub.Object.GetQuery("SomeClass");
        Assert.IsNotNull(query);
        Assert.AreEqual("SomeClass", query.ClassName);
    }

    [TestMethod]
    public async Task ConnectLiveQueryServerAsync_AttachesHandlerAndConnects()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseLiveQueryController> mockLq = new Mock<IParseLiveQueryController>();
        mockLq.Setup(l => l.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        mockHub.Setup(h => h.LiveQueryController).Returns(mockLq.Object);

        bool called = false;
        EventHandler<ParseLiveQueryErrorEventArgs> handler = (s, e) => { called = true; };
        await mockHub.Object.ConnectLiveQueryServerAsync(handler);

        // simulate event fire
        mockLq.Raise(l => l.Error += null, new object(), new ParseLiveQueryErrorEventArgs(0, "error", false));
        Assert.IsTrue(called);
        mockLq.Verify(l => l.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task DisconnectLiveQueryServerAsync_CallsClose()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseLiveQueryController> mockLq = new Mock<IParseLiveQueryController>();
        mockLq.Setup(l => l.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        mockHub.Setup(h => h.LiveQueryController).Returns(mockLq.Object);

        await mockHub.Object.DisconnectLiveQueryServerAsync();
        mockLq.Verify(l => l.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void CanBeSerializedAsValue_TrueWhenAllIds()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        ParseObject p = new ParseObject("MyClass") { ObjectId = "id" };
        Assert.IsTrue(mockHub.Object.CanBeSerializedAsValue(p));
    }

    [TestMethod]
    public void CanBeSerializedAsValue_FalseWhenMissingId()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        ParseObject p = new ParseObject("MyClass");
        Assert.IsFalse(mockHub.Object.CanBeSerializedAsValue(p));
    }

    [TestMethod]
    public void GetFieldForPropertyName_NullServiceHub_ReturnsNull()
    {
        IServiceHub hub = null;
        Assert.IsNull(hub.GetFieldForPropertyName("cls", "prop"));
    }

    [TestMethod]
    public void GetFieldForPropertyName_ThrowsOnEmptyClassName()
    {
        IServiceHub hub = new Mock<IServiceHub>().Object;
        Assert.ThrowsException<ArgumentException>(() => hub.GetFieldForPropertyName("", "prop"));
    }

    [TestMethod]
    public void GetFieldForPropertyName_ThrowsOnEmptyPropertyName()
    {
        IServiceHub hub = new Mock<IServiceHub>().Object;
        Assert.ThrowsException<ArgumentException>(() => hub.GetFieldForPropertyName("cls", ""));
    }

    [TestMethod]
    public void GetFieldForPropertyName_ThrowsOnNullController()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        mockHub.Setup(h => h.ClassController).Returns((IParseObjectClassController) null);
        Assert.ThrowsException<InvalidOperationException>(() => mockHub.Object.GetFieldForPropertyName("cls", "prop"));
    }

    [TestMethod]
    public void GetFieldForPropertyName_ThrowsOnNullMappings()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockController.Setup(c => c.GetPropertyMappings("cls")).Returns((IDictionary<string, string>) null);
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);
        Assert.ThrowsException<InvalidOperationException>(() => mockHub.Object.GetFieldForPropertyName("cls", "prop"));
    }

    [TestMethod]
    public void GetFieldForPropertyName_ThrowsOnPropertyNotFound()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockController.Setup(c => c.GetPropertyMappings("cls")).Returns(new Dictionary<string, string>());
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);
        Assert.ThrowsException<KeyNotFoundException>(() => mockHub.Object.GetFieldForPropertyName("cls", "prop"));
    }

    [TestMethod]
    public void GetFieldForPropertyName_ReturnsExpectedMapping()
    {
        Mock<IServiceHub> mockHub = new Mock<IServiceHub>();
        Mock<IParseObjectClassController> mockController = new Mock<IParseObjectClassController>();
        mockController.Setup(c => c.GetPropertyMappings("cls")).Returns(new Dictionary<string, string> { { "prop", "field" } });
        mockHub.Setup(h => h.ClassController).Returns(mockController.Object);
        Assert.AreEqual("field", mockHub.Object.GetFieldForPropertyName("cls", "prop"));
    }
}
