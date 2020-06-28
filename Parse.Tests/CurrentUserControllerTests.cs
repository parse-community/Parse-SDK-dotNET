using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Infrastructure;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;
using Parse.Platform.Objects;
using Parse.Platform.Users;

namespace Parse.Tests
{
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
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestGetSetAsync()
        {
#warning This method may need a fully custom ParseClient setup.

            Mock<ICacheController> storageController = new Mock<ICacheController>(MockBehavior.Strict);
            Mock<IDataCache<string, object>> mockedStorage = new Mock<IDataCache<string, object>>();

            ParseCurrentUserController controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);

            ParseUser user = new ParseUser { }.Bind(Client) as ParseUser;

            storageController.Setup(storage => storage.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

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

                mockedStorage.Verify(storage => storage.AddAsync("CurrentUser", Match.Create(predicate)));
                mockedStorage.Setup(storage => storage.TryGetValue("CurrentUser", out jsonObject)).Returns(true);

                return controller.GetAsync(Client, CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.AreEqual(user, controller.CurrentUser);

                controller.ClearFromMemory();
                Assert.AreNotEqual(user, controller.CurrentUser);

                return controller.GetAsync(Client, CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.AreNotSame(user, controller.CurrentUser);
                Assert.IsNotNull(controller.CurrentUser);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestExistsAsync()
        {
            Mock<ICacheController> storageController = new Mock<ICacheController>();
            Mock<IDataCache<string, object>> mockedStorage = new Mock<IDataCache<string, object>>();
            ParseCurrentUserController controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);
            ParseUser user = new ParseUser { }.Bind(Client) as ParseUser;

            storageController.Setup(c => c.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

            bool contains = false;
            mockedStorage.Setup(storage => storage.AddAsync("CurrentUser", It.IsAny<object>())).Callback(() => contains = true).Returns(Task.FromResult<object>(null)).Verifiable();

            mockedStorage.Setup(storage => storage.RemoveAsync("CurrentUser")).Callback(() => contains = false).Returns(Task.FromResult<object>(null)).Verifiable();

            mockedStorage.Setup(storage => storage.ContainsKey("CurrentUser")).Returns(() => contains);

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ =>
            {
                Assert.AreEqual(user, controller.CurrentUser);

                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.IsTrue(task.Result);

                controller.ClearFromMemory();
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.IsTrue(task.Result);

                controller.ClearFromDisk();
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.IsFalse(task.Result);
                mockedStorage.Verify();
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestIsCurrent()
        {
            Mock<ICacheController> storageController = new Mock<ICacheController>(MockBehavior.Strict);
            ParseCurrentUserController controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);

            ParseUser user = new ParseUser { }.Bind(Client) as ParseUser;
            ParseUser user2 = new ParseUser { }.Bind(Client) as ParseUser;

            storageController.Setup(storage => storage.LoadAsync()).Returns(Task.FromResult(new Mock<IDataCache<string, object>>().Object));

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(task =>
            {
                Assert.IsTrue(controller.IsCurrent(user));
                Assert.IsFalse(controller.IsCurrent(user2));

                controller.ClearFromMemory();

                Assert.IsFalse(controller.IsCurrent(user));

                return controller.SetAsync(user, CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.IsTrue(controller.IsCurrent(user));
                Assert.IsFalse(controller.IsCurrent(user2));

                controller.ClearFromDisk();

                Assert.IsFalse(controller.IsCurrent(user));

                return controller.SetAsync(user2, CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.IsFalse(controller.IsCurrent(user));
                Assert.IsTrue(controller.IsCurrent(user2));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CurrentUserControllerTests))]
        public Task TestCurrentSessionToken()
        {
            Mock<ICacheController> storageController = new Mock<ICacheController>();
            Mock<IDataCache<string, object>> mockedStorage = new Mock<IDataCache<string, object>>();
            ParseCurrentUserController controller = new ParseCurrentUserController(storageController.Object, Client.ClassController, Client.Decoder);

            storageController.Setup(c => c.LoadAsync()).Returns(Task.FromResult(mockedStorage.Object));

            return controller.GetCurrentSessionTokenAsync(Client, CancellationToken.None).OnSuccess(task =>
            {
                Assert.IsNull(task.Result);

                // We should probably mock this.

                ParseUser user = Client.CreateObjectWithoutData<ParseUser>(default);
                user.HandleFetchResult(new MutableObjectState { ServerData = new Dictionary<string, object> { ["sessionToken"] = "randomString" } });

                return controller.SetAsync(user, CancellationToken.None);
            }).Unwrap().OnSuccess(_ => controller.GetCurrentSessionTokenAsync(Client, CancellationToken.None)).Unwrap().OnSuccess(task => Assert.AreEqual("randomString", task.Result));
        }

        public Task TestLogOut()
        {
            ParseCurrentUserController controller = new ParseCurrentUserController(new Mock<ICacheController>(MockBehavior.Strict).Object, Client.ClassController, Client.Decoder);
            ParseUser user = new ParseUser { }.Bind(Client) as ParseUser;

            return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ =>
            {
                Assert.AreEqual(user, controller.CurrentUser);
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap().OnSuccess(task =>
            {
                Assert.IsTrue(task.Result);
                return controller.LogOutAsync(Client, CancellationToken.None);
            }).Unwrap().OnSuccess(_ => controller.GetAsync(Client, CancellationToken.None)).Unwrap().OnSuccess(task =>
            {
                Assert.IsNull(task.Result);
                return controller.ExistsAsync(CancellationToken.None);
            }).Unwrap().OnSuccess(t => Assert.IsFalse(t.Result));
        }
    }
}
