using Parse;
using Parse.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class CurrentUserControllerTests {
    [SetUp]
    public void SetUp() {
      ParseObject.RegisterSubclass<ParseUser>();
    }

    [TearDown]
    public void TearDown() {
      ParseObject.UnregisterSubclass<ParseUser>();
    }

    [Test]
    public void TestConstructor() {
      var controller = new ParseCurrentUserController();
      Assert.IsNull(controller.CurrentUser);
    }

    [Test]
    [AsyncStateMachine(typeof(CurrentUserControllerTests))]
    public Task TestGetSetAsync() {
      var controller = new ParseCurrentUserController();
      var user = new ParseUser();

      return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ => {
        Assert.AreEqual(user, controller.CurrentUser);

        return controller.GetAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.AreEqual(user, controller.CurrentUser);

        controller.ClearFromMemory();
        Assert.AreNotEqual(user, controller.CurrentUser);

        return controller.GetAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.AreNotSame(user, controller.CurrentUser);
        Assert.IsNotNull(controller.CurrentUser);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CurrentUserControllerTests))]
    public Task TestExistsAsync() {
      var controller = new ParseCurrentUserController();
      var user = new ParseUser();

      return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ => {
        Assert.AreEqual(user, controller.CurrentUser);
        return controller.ExistsAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsTrue(t.Result);

        controller.ClearFromMemory();

        return controller.ExistsAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsTrue(t.Result);

        controller.ClearFromDisk();

        return controller.ExistsAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsFalse(t.Result);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CurrentUserControllerTests))]
    public Task TestIsCurrent() {
      var controller = new ParseCurrentUserController();
      var user = new ParseUser();
      var user2 = new ParseUser();

      return controller.SetAsync(user, CancellationToken.None).OnSuccess(t => {
        Assert.IsTrue(controller.IsCurrent(user));
        Assert.IsFalse(controller.IsCurrent(user2));

        controller.ClearFromMemory();

        Assert.IsFalse(controller.IsCurrent(user));

        return controller.SetAsync(user, CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsTrue(controller.IsCurrent(user));
        Assert.IsFalse(controller.IsCurrent(user2));

        controller.ClearFromDisk();

        Assert.IsFalse(controller.IsCurrent(user));

        return controller.SetAsync(user2, CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsFalse(controller.IsCurrent(user));
        Assert.IsTrue(controller.IsCurrent(user2));
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CurrentUserControllerTests))]
    public Task TestCurrentSessionToken() {
      var controller = new ParseCurrentUserController();

      return controller.GetCurrentSessionTokenAsync(CancellationToken.None).OnSuccess(t => {
        Assert.IsNull(t.Result);

        // We should probably mock this.
        var userState = new MutableObjectState {
          ServerData = new Dictionary<string, object>() {
            { "sessionToken", "randomString" }
          }
        };
        var user = ParseObject.CreateWithoutData<ParseUser>(null);
        user.HandleFetchResult(userState);

        return controller.SetAsync(user, CancellationToken.None);
      }).Unwrap()
      .OnSuccess(_ => controller.GetCurrentSessionTokenAsync(CancellationToken.None)).Unwrap()
      .OnSuccess(t => {
        Assert.AreEqual("randomString", t.Result);
      });
    }

    public Task TestLogOut() {
      var controller = new ParseCurrentUserController();
      var user = new ParseUser();

      return controller.SetAsync(user, CancellationToken.None).OnSuccess(_ => {
        Assert.AreEqual(user, controller.CurrentUser);
        return controller.ExistsAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsTrue(t.Result);

        return controller.LogOutAsync(CancellationToken.None);
      }).Unwrap().OnSuccess(_ => controller.GetAsync(CancellationToken.None)).Unwrap()
      .OnSuccess(t => {
        Assert.IsNull(t.Result);

        return controller.ExistsAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsFalse(t.Result);
      });
    }
  }
}
