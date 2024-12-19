using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Users;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Infrastructure.Data;

namespace Parse.Tests;

[TestClass]
public class CurrentUserControllerTests
{
    private ParseClient Client;

    public CurrentUserControllerTests()
    {
        // Mock the decoder
        var mockDecoder = new Mock<IParseDataDecoder>();

        // Mock the class controller
        var mockClassController = new Mock<IParseObjectClassController>();

        // Ensure that the base implementation of Instantiate is called
        mockClassController.Setup(controller => controller.Instantiate(It.IsAny<string>(), It.IsAny<IServiceHub>()))
            .CallBase();

        // Mock the service hub
        var mockServiceHub = new Mock<IServiceHub>();
        mockServiceHub.SetupGet(hub => hub.Decoder).Returns(mockDecoder.Object);
        mockServiceHub.SetupGet(hub => hub.ClassController).Returns(mockClassController.Object);

        // Initialize ParseClient with the mocked ServiceHub
        Client = new ParseClient(new ServerConnectionData { Test = true }, mockServiceHub.Object);

        // Call Publicize() to make the client instance accessible globally
        Client.Publicize(); // This makes ParseClient.Instance point to this instance

        // Ensure the ParseUser class is valid for this client instance
        Client.AddValidClass<ParseUser>();
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Client.Services is ServiceHub serviceHub)
        {
            serviceHub.Reset();
        }
    }



    [TestMethod]
    public void TestConstructor()
    {
        // Mock the IParseObjectClassController
        var mockClassController = new Mock<IParseObjectClassController>();

        // Create the controller with the mock classController
        var controller = new ParseCurrentUserController(
            new Mock<ICacheController>().Object,
            mockClassController.Object,
            Client.Decoder
        );

        // Now the test should pass as the classController is mocked
        Assert.IsNull(controller.CurrentUser);
    }

    [TestMethod]
    public async Task TestGetSetAsync()
    {
        // Mock setup for storage
        var storageController = new Mock<ICacheController>(MockBehavior.Strict);
        var mockedStorage = new Mock<IDataCache<string, object>>();

        storageController
            .Setup(storage => storage.LoadAsync())
            .ReturnsAsync(mockedStorage.Object);

        object capturedSerializedData = null;

        mockedStorage
            .Setup(storage => storage.AddAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((key, value) =>
            {
                if (key == "CurrentUser" && value is string serialized)
                {
                    // Capture the serialized data
                    capturedSerializedData = serialized;
                }
            })
            .Returns(Task.CompletedTask);

        // Mock RemoveAsync
        mockedStorage
            .Setup(storage => storage.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Mock TryGetValue to return capturedSerializedData
        mockedStorage
            .Setup(storage => storage.TryGetValue("CurrentUser", out capturedSerializedData))
            .Returns((string key, out object value) =>
            {
                value = capturedSerializedData; // Assign the captured serialized data to the out parameter
                return value != null;
            });

        // Mock ClassController behavior
        var classControllerMock = new Mock<IParseObjectClassController>();
        classControllerMock.Setup(controller => controller.Instantiate(It.IsAny<string>(), It.IsAny<IServiceHub>()))
            .Returns<string, IServiceHub>((className, serviceHub) => new ParseUser { ObjectId = "testObjectId" });

        var controller = new ParseCurrentUserController(storageController.Object, classControllerMock.Object, Client.Decoder);

        // The ParseUser will automatically be bound to ParseClient.Instance
        var user = new ParseUser { ObjectId = "testObjectId" };

        // Perform SetAsync operation
        await controller.SetAsync(user, CancellationToken.None);

        // Assertions
        Assert.AreEqual(user, controller.CurrentUser);

        // Verify AddAsync was called
        mockedStorage.Verify(storage => storage.AddAsync("CurrentUser", It.IsAny<object>()), Times.Once);

        // Perform GetAsync operation
        var retrievedUser = await controller.GetAsync(Client, CancellationToken.None);
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(user.ObjectId, retrievedUser.ObjectId);

        // Clear user from memory
        controller.ClearFromMemory();
        Assert.AreNotEqual(user, controller.CurrentUser); // Ensure the user is no longer in memory

        // Retrieve user again
        retrievedUser = await controller.GetAsync(Client, CancellationToken.None);
        Assert.AreNotSame(user, retrievedUser); // Ensure the user is not the same instance
        Assert.IsNotNull(controller.CurrentUser); // Ensure the CurrentUser is not null after re-fetching
    }

    [TestMethod]
    public async Task TestExistsAsync()
    {
        // Mock setup
        var storageController = new Mock<ICacheController>();
        var mockedStorage = new Mock<IDataCache<string, object>>();
        var controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);
        var user = new ParseUser().Bind(Client) as ParseUser;

        storageController
            .Setup(c => c.LoadAsync())
            .ReturnsAsync(mockedStorage.Object);

        bool contains = false;

        mockedStorage
           .Setup(storage => storage.AddAsync("CurrentUser", It.IsAny<object>()))
           .Callback(() => contains = true)
           .Returns(() => Task.FromResult((object) null))
           .Verifiable();

        mockedStorage
            .Setup(storage => storage.RemoveAsync("CurrentUser"))
            .Callback(() => contains = false)
            .Returns(() => Task.FromResult((object) null))
            .Verifiable();


        mockedStorage
            .Setup(storage => storage.ContainsKey("CurrentUser"))
            .Returns(() => contains);

        // Perform SetAsync operation
        await controller.SetAsync(user, CancellationToken.None);

        // Assert that the current user is set correctly
        Assert.AreEqual(user, controller.CurrentUser);

        // Check if the user exists
        var exists = await controller.ExistsAsync(CancellationToken.None);
        Assert.IsTrue(exists);

        // Clear from memory and re-check existence
        controller.ClearFromMemory();
        exists = await controller.ExistsAsync(CancellationToken.None);
        Assert.IsTrue(exists);

        // Clear from disk and re-check existence
        await controller.ClearFromDiskAsync();
        exists = await controller.ExistsAsync(CancellationToken.None);
        Assert.IsFalse(exists);

        // Verify mocked behavior
        mockedStorage.Verify();
    }

    [TestMethod]
    public async Task TestIsCurrent()
    {
        var storageController = new Mock<ICacheController>(MockBehavior.Strict);
        var controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);

        var user = new ParseUser().Bind(Client) as ParseUser;
        var user2 = new ParseUser().Bind(Client) as ParseUser;

        storageController
            .Setup(storage => storage.LoadAsync())
            .ReturnsAsync(new Mock<IDataCache<string, object>>().Object);

        // Set the first user
        await controller.SetAsync(user, CancellationToken.None);

        Assert.IsTrue(controller.IsCurrent(user));
        Assert.IsFalse(controller.IsCurrent(user2));

        // Clear from memory and verify
        controller.ClearFromMemory();
        Assert.IsFalse(controller.IsCurrent(user));

        // Re-set the first user
        await controller.SetAsync(user, CancellationToken.None);

        Assert.IsTrue(controller.IsCurrent(user));
        Assert.IsFalse(controller.IsCurrent(user2));

        // Clear from disk and verify
        await controller.ClearFromDiskAsync();
        Assert.IsFalse(controller.IsCurrent(user));

        // Set the second user and verify
        await controller.SetAsync(user2, CancellationToken.None);

        Assert.IsFalse(controller.IsCurrent(user));
        Assert.IsTrue(controller.IsCurrent(user2));
    }

}
