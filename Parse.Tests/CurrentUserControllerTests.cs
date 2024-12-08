using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Users;

namespace Parse.Tests;

[TestClass]
public class CurrentUserControllerTests
{
    ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    [TestInitialize]
    public void SetUp() => Client.AddValidClass<ParseUser>();

    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestConstructor() => Assert.IsNull(new ParseCurrentUserController(new Mock<ICacheController> { }.Object, Client.ClassController, Client.Decoder).CurrentUser);

    [TestMethod]
    
    public async Task TestGetSetAsync()
    {
#warning This method may need a fully custom ParseClient setup.

        // Mock setup
        var storageController = new Mock<ICacheController>(MockBehavior.Strict);
        var mockedStorage = new Mock<IDataCache<string, object>>();

        var controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);

        var user = new ParseUser().Bind(Client) as ParseUser;

        storageController
            .Setup(storage => storage.LoadAsync())
            .ReturnsAsync(mockedStorage.Object);

        // Perform SetAsync operation
        await controller.SetAsync(user, CancellationToken.None);

        // Assertions
        Assert.AreEqual(user, controller.CurrentUser);

        object jsonObject = null;
#pragma warning disable IDE0039 // Use local function
        Predicate<object> predicate = o =>
        {
            jsonObject = o;
            return true;
        };
#pragma warning restore IDE0039 // Use local function

        mockedStorage.Verify(storage => storage.AddAsync("CurrentUser", Match.Create(predicate)));
        mockedStorage
            .Setup(storage => storage.TryGetValue("CurrentUser", out jsonObject))
            .Returns(true);

        // Perform GetAsync operation
        var retrievedUser = await controller.GetAsync(Client, CancellationToken.None);
        Assert.AreEqual(user, retrievedUser);

        // Clear user from memory
        controller.ClearFromMemory();
        Assert.AreNotEqual(user, controller.CurrentUser);

        // Retrieve user again
        retrievedUser = await controller.GetAsync(Client, CancellationToken.None);
        Assert.AreNotSame(user, retrievedUser);
        Assert.IsNotNull(controller.CurrentUser);
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
