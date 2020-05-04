using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Installations;

namespace Parse.Tests
{
#warning Class refactoring may be required.

    [TestClass]
    public class InstallationIdControllerTests
    {
        ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

        [TestCleanup]
        public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestConstructor()
        {
            Mock<ICacheController> storageMock = new Mock<ICacheController>(MockBehavior.Strict);
            ParseInstallationController controller = new ParseInstallationController(storageMock.Object);

            // Make sure it didn't touch storageMock.

            storageMock.Verify();
        }

        [TestMethod]
        [AsyncStateMachine(typeof(InstallationIdControllerTests))]
        public Task TestGet()
        {
            Mock<ICacheController> storageMock = new Mock<ICacheController>(MockBehavior.Strict);
            Mock<IDataCache<string, object>> storageDictionary = new Mock<IDataCache<string, object>>();

            storageMock.Setup(s => s.LoadAsync()).Returns(Task.FromResult(storageDictionary.Object));

            ParseInstallationController controller = new ParseInstallationController(storageMock.Object);
            return controller.GetAsync().ContinueWith(installationIdTask =>
            {
                Assert.IsFalse(installationIdTask.IsFaulted);

                object verified = null;
                storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
                storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));

                return controller.GetAsync().ContinueWith(newInstallationIdTask =>
                {
                    Assert.IsFalse(newInstallationIdTask.IsFaulted);

                    // Ensure nothing more has happened with our dictionary.
                    storageDictionary.VerifyAll();

                    Assert.AreEqual(installationIdTask.Result, newInstallationIdTask.Result);

                    return controller.ClearAsync();
                }).Unwrap().ContinueWith(clearTask =>
                {
                    Assert.IsFalse(clearTask.IsFaulted);

                    storageDictionary.Verify(storage => storage.RemoveAsync("InstallationId"));

                    return controller.GetAsync();
                }).Unwrap().ContinueWith(newInstallationIdTask =>
                {
                    Assert.IsFalse(newInstallationIdTask.IsFaulted);

                    Assert.AreNotEqual(installationIdTask.Result, newInstallationIdTask.Result);

                    storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
                    storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));
                });
            }).Unwrap();
        }

        [TestMethod]
        [AsyncStateMachine(typeof(InstallationIdControllerTests))]
        public Task TestSet()
        {
            Mock<ICacheController> storageMock = new Mock<ICacheController>(MockBehavior.Strict);
            Mock<IDataCache<string, object>> storageDictionary = new Mock<IDataCache<string, object>>();

            storageMock.Setup(s => s.LoadAsync()).Returns(Task.FromResult(storageDictionary.Object));

            ParseInstallationController controller = new ParseInstallationController(storageMock.Object);

            return controller.GetAsync().ContinueWith(installationIdTask =>
            {
                Assert.IsFalse(installationIdTask.IsFaulted);

                object verified = null;
                storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));
                storageDictionary.Verify(s => s.AddAsync("InstallationId", It.IsAny<object>()));

                Guid? installationId = installationIdTask.Result;
                Guid installationId2 = Guid.NewGuid();

                return controller.SetAsync(installationId2).ContinueWith(setTask =>
                {
                    Assert.IsFalse(setTask.IsFaulted);

                    storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId2.ToString()));

                    return controller.GetAsync();
                }).Unwrap().ContinueWith(installationId3Task =>
                {
                    Assert.IsFalse(installationId3Task.IsFaulted);

                    storageDictionary.Verify(s => s.TryGetValue("InstallationId", out verified));

                    Guid? installationId3 = installationId3Task.Result;
                    Assert.AreEqual(installationId2, installationId3);

                    return controller.SetAsync(installationId);
                }).Unwrap().ContinueWith(setTask =>
                {
                    Assert.IsFalse(setTask.IsFaulted);

                    storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId.ToString()));

                    return controller.ClearAsync();
                }).Unwrap().ContinueWith(clearTask =>
                {
                    Assert.IsFalse(clearTask.IsFaulted);

                    storageDictionary.Verify(s => s.RemoveAsync("InstallationId"));

                    return controller.SetAsync(installationId2);
                }).Unwrap().ContinueWith(setTask =>
                {
                    Assert.IsFalse(setTask.IsFaulted);

                    storageDictionary.Verify(s => s.AddAsync("InstallationId", installationId2.ToString()));
                });
            }).Unwrap();
        }
    }
}
