using Parse;
using Parse.Core.Internal;
using Moq;
using NUnit.Framework;
using System;
using Parse.Common.Internal;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace ParseTest {
  [TestFixture]
  public class InstallationIdControllerTests {
    [TearDown]
    public void TearDown() {
      ParseCorePlugins.Instance = null;
    }

    [Test]
    public void TestConstructor() {
      var storageMock = new Mock<IStorageController>(MockBehavior.Strict);
      var controller = new InstallationIdController(storageMock.Object);

      // Make sure it didn't touch storageMock.
      storageMock.Verify();
    }

    [Test]
    [AsyncStateMachine(typeof(InstallationIdControllerTests))]
    public Task TestGet() {
      var storageMock = new Mock<IStorageController>(MockBehavior.Strict);
      var storageDictionary = new Mock<IStorageDictionary<string, object>>();

      storageMock.Setup(s => s.LoadAsync()).Returns(Task.FromResult(storageDictionary.Object));

      var controller = new InstallationIdController(storageMock.Object);
      return controller.GetAsync().ContinueWith(installationIdTask => {
        Assert.False(installationIdTask.IsFaulted);

        object verified = null;
        storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
        storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));

        return controller.GetAsync().ContinueWith(newInstallationIdTask => {
          Assert.False(newInstallationIdTask.IsFaulted);

          // Ensure nothing more has happened with our dictionary.
          storageDictionary.VerifyAll();

          Assert.AreEqual(installationIdTask.Result, newInstallationIdTask.Result);

          return controller.ClearAsync();
        }).Unwrap().ContinueWith(clearTask => {
          Assert.False(clearTask.IsFaulted);

          storageDictionary.Verify(storage => storage.RemoveAsync("InstallationId"));

          return controller.GetAsync();
        }).Unwrap().ContinueWith(newInstallationIdTask => {
          Assert.False(newInstallationIdTask.IsFaulted);

          Assert.AreNotEqual(installationIdTask.Result, newInstallationIdTask.Result);

          storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
          storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));
        });
      }).Unwrap();
    }

    [Test]
    [AsyncStateMachine(typeof(InstallationIdControllerTests))]
    public Task TestSet() {
      var storageMock = new Mock<IStorageController>(MockBehavior.Strict);
      var storageDictionary = new Mock<IStorageDictionary<string, object>>();

      storageMock.Setup(s => s.LoadAsync()).Returns(Task.FromResult(storageDictionary.Object));

      var controller = new InstallationIdController(storageMock.Object);

      return controller.GetAsync().ContinueWith(installationIdTask => {
        Assert.False(installationIdTask.IsFaulted);

        object verified = null;
        storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
        storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));

        var installationId = installationIdTask.Result;
        var installationId2 = Guid.NewGuid();

        return controller.SetAsync(installationId2).ContinueWith(setTask => {
          Assert.False(setTask.IsFaulted);

          storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId2.ToString()));

          return controller.GetAsync();
        }).Unwrap().ContinueWith(installationId3Task => {
          Assert.False(installationId3Task.IsFaulted);

          storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));

          var installationId3 = installationId3Task.Result;
          Assert.AreEqual(installationId2, installationId3);

          return controller.SetAsync(installationId);
        }).Unwrap().ContinueWith(setTask => {
          Assert.False(setTask.IsFaulted);

          storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId.ToString()));

          return controller.ClearAsync();
        }).Unwrap().ContinueWith(clearTask => {
          Assert.False(clearTask.IsFaulted);

          storageDictionary.Verify(s => s.RemoveAsync("InstallationId"));

          return controller.SetAsync(installationId2);
        }).Unwrap().ContinueWith(setTask => {
          Assert.False(setTask.IsFaulted);

          storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId2.ToString()));
        });
      }).Unwrap();
    }
  }
}
