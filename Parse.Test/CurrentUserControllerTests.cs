using Moq;
using Parse;
using Parse.Common.Internal;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class CurrentUserControllerTests
    {
        [TestInitialize]
        public void SetUp() => ParseObject.RegisterSubclass<ParseUser>();

        [TestCleanup]
        public void TearDown() => ParseCorePlugins.Instance.Reset();

        [TestMethod]
        public void TestConstructor() => Assert.IsNull(new ParseCurrentUserController(new Mock<IStorageController>().Object).CurrentUser);

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestGetSetAsync()
        {
            var storageController = new Mock<IStorageController>(MockBehavior.Strict);
            var mockedStorage = new Mock<IStorageDictionary<string, object>>();
            var controller = new ParseCurrentUserController(storageController.Object);
            var user = new ParseUser();

            storageController.Setup(s => s.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ =>
            {
                Assert.AreEqual(user, controller.CurrentUser);

                object jsonObject = null;
#pragma warning disable IDE0039 // Use local function
                Predicate<object> predicate = o =>
                {
                    jsonObject = o;
                    return true;
                };
#pragma warning restore IDE0039 // Use local function

                mockedStorage.Verify(s => s.AddAsync("CurrentUser", Match.Create(predicate)));
                mockedStorage.Setup(s => s.TryGetValue("CurrentUser", out jsonObject)).Returns(true);

                return controller.GetAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.AreEqual(user, controller.CurrentUser);

                controller.ClearFromMemory();
                Assert.AreNotEqual(user, controller.CurrentUser);

                return controller.GetAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.AreNotSame(user, controller.CurrentUser);
                Assert.IsNotNull(controller.CurrentUser);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestExistsAsync()
        {
            var storageController = new Mock<IStorageController>();
            var mockedStorage = new Mock<IStorageDictionary<string, object>>();
            var controller = new ParseCurrentUserController(storageController.Object);
            var user = new ParseUser();

            storageController.Setup(c => c.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

            bool contains = false;
            mockedStorage.Setup(s => s.AddAsync("CurrentUser", It.IsAny<object>())).Callback(() =>
            {
                contains = true;
            }).Returns(Task.FromResult<object>(null)).Verifiable();

            mockedStorage.Setup(s => s.RemoveAsync("CurrentUser")).Callback(() =>
            {
                contains = false;
            }).Returns(Task.FromResult<object>(null)).Verifiable();

            mockedStorage.Setup(s => s.ContainsKey("CurrentUser")).Returns(() => contains);

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ =>
            {
                Assert.AreEqual(user, controller.CurrentUser);

                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsTrue(t.Result);

                controller.ClearFromMemory();
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsTrue(t.Result);

                controller.ClearFromDisk();
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsFalse(t.Result);
                mockedStorage.Verify();
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestIsCurrent()
        {
            var storageController = new Mock<IStorageController>(MockBehavior.Strict);
            var mockedStorage = new Mock<IStorageDictionary<string, object>>();
            var controller = new ParseCurrentUserController(storageController.Object);
            var user = new ParseUser();
            var user2 = new ParseUser();

            storageController.Setup(s => s.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(t =>
            {
                Assert.IsTrue(controller.IsCurrent(user));
                Assert.IsFalse(controller.IsCurrent(user2));

                controller.ClearFromMemory();

                Assert.IsFalse(controller.IsCurrent(user));

                return controller.SetAsync(user, CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsTrue(controller.IsCurrent(user));
                Assert.IsFalse(controller.IsCurrent(user2));

                controller.ClearFromDisk();

                Assert.IsFalse(controller.IsCurrent(user));

                return controller.SetAsync(user2, CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsFalse(controller.IsCurrent(user));
                Assert.IsTrue(controller.IsCurrent(user2));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestCurrentSessionToken()
        {
            var storageController = new Mock<IStorageController>();
            var mockedStorage = new Mock<IStorageDictionary<string, object>>();
            var controller = new ParseCurrentUserController(storageController.Object);

            storageController.Setup(c => c.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

            return controller.GetCurrentSessionTokenAsync(CancellationToken.None).OnSuccess(t =>
            {
                Assert.IsNull(t.Result);

                // We should probably mock this.
                var userState = new MutableObjectState
                {
                    ServerData = new Dictionary<string, object>() {
            { "sessionToken", "randomString" }
          }
                };
                var user = ParseObject.CreateWithoutData<ParseUser>(null);
                user.HandleFetchResult(userState);

                return controller.SetAsync(user, CancellationToken.None);
            }).Unwrap()
            .OnSuccess(_ => controller.GetCurrentSessionTokenAsync(CancellationToken.None)).Unwrap()
            .OnSuccess(t =>
            {
                Assert.AreEqual("randomString", t.Result);
            });
        }

        public Task TestLogOut()
        {
            var storageController = new Mock<IStorageController>(MockBehavior.Strict);
            var controller = new ParseCurrentUserController(storageController.Object);
            var user = new ParseUser();

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ =>
            {
                Assert.AreEqual(user, controller.CurrentUser);
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsTrue(t.Result);

                return controller.LogOutAsync(CancellationToken.None);
            }).Unwrap().OnSuccess(_ => controller.GetAsync(CancellationToken.None)).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsNull(t.Result);

                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap()
            .OnSuccess(t =>
            {
                Assert.IsFalse(t.Result);
            });
        }
    }
}
