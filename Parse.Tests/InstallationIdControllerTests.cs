using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Installations;

namespace Parse.Tests;

[TestClass]
public class InstallationIdControllerTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestConstructor()
    {
        var storageMock = new Mock<ICacheController>(MockBehavior.Strict);
        var controller = new ParseInstallationController(storageMock.Object);

        // Ensure no interactions with the storageMock.
        storageMock.Verify();
    }

    [TestMethod]
    public async Task TestGetAsync()
    {
        var storageMock = new Mock<ICacheController>(MockBehavior.Strict);
        var storageDictionary = new Mock<IDataCache<string, object>>();

        storageMock.Setup(s => s.LoadAsync()).ReturnsAsync(storageDictionary.Object);

        var controller = new ParseInstallationController(storageMock.Object);

        // Initial Get
        var installationId = await controller.GetAsync();
        Assert.IsNotNull(installationId);

        object verified = null;
        storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
        storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));

        // Second Get - Ensure same ID
        var newInstallationId = await controller.GetAsync();
        Assert.AreEqual(installationId, newInstallationId);
        storageDictionary.VerifyAll();

        // Clear and ensure new ID
        await controller.ClearAsync();
        storageDictionary.Verify(s => s.RemoveAsync("InstallationId"));

        var clearedInstallationId = await controller.GetAsync();
        Assert.AreNotEqual(installationId, clearedInstallationId);
        storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
        storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));
    }

    [TestMethod]
    public async Task TestSetAsync()
    {
        var storageMock = new Mock<ICacheController>(MockBehavior.Strict);
        var storageDictionary = new Mock<IDataCache<string, object>>();

        storageMock.Setup(s => s.LoadAsync()).ReturnsAsync(storageDictionary.Object);

        var controller = new ParseInstallationController(storageMock.Object);

        // Initial Get
        var installationId = await controller.GetAsync();
        Assert.IsNotNull(installationId);

        object verified = null;
        storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
        storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));

        // Set a new Installation ID
        var newInstallationId = Guid.NewGuid();
        await controller.SetAsync(newInstallationId);
        storageDictionary.Verify(s => s.AddAsync("InstallationId", newInstallationId.ToString()));

        // Verify Set ID matches Get
        var retrievedInstallationId = await controller.GetAsync();
        Assert.AreEqual(newInstallationId, retrievedInstallationId);

        // Reset to original Installation ID
        await controller.SetAsync(installationId);
        storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId.ToString()));

        // Clear and set new ID
        await controller.ClearAsync();
        storageDictionary.Verify(s => s.RemoveAsync("InstallationId"));

        await controller.SetAsync(newInstallationId);
        storageDictionary.Verify(s => s.AddAsync("InstallationId", newInstallationId.ToString()));
    }
}
