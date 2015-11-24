using Parse;
using Parse.Internal;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace ParseTest {
  [TestFixture]
  public class CurrentInstallationControllerTests {
    [SetUp]
    public void SetUp() {
      ParseObject.RegisterSubclass<ParseInstallation>();
    }

    [TearDown]
    public void TearDown() {
      ParseObject.UnregisterSubclass<ParseInstallation>();
    }

    [Test]
    public void TestConstructor() {
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var guid = Guid.NewGuid();
      mockInstallationIdController.Setup(obj => obj.Get()).Returns(guid);

      var controller = new ParseCurrentInstallationController(new Mock<IInstallationIdController>().Object);
      Assert.IsNull(controller.CurrentInstallation);
    }

    [Test]
    [AsyncStateMachine(typeof(CurrentInstallationControllerTests))]
    public Task TestGetSetAsync() {
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var guid = Guid.NewGuid();
      mockInstallationIdController.Setup(obj => obj.Get()).Returns(guid);

      var controller = new ParseCurrentInstallationController(mockInstallationIdController.Object);
      var installation = new ParseInstallation();

      return controller.SetAsync(installation, CancellationToken.None).OnSuccess(_ => {
        Assert.AreEqual(installation, controller.CurrentInstallation);

        return controller.GetAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.AreEqual(installation, controller.CurrentInstallation);

        controller.ClearFromMemory();
        Assert.AreNotEqual(installation, controller.CurrentInstallation);

        return controller.GetAsync(CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.AreNotSame(installation, controller.CurrentInstallation);
        Assert.IsNotNull(controller.CurrentInstallation);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CurrentInstallationControllerTests))]
    public Task TestExistsAsync() {
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var guid = Guid.NewGuid();
      mockInstallationIdController.Setup(obj => obj.Get()).Returns(guid);

      var controller = new ParseCurrentInstallationController(mockInstallationIdController.Object);
      var installation = new ParseInstallation();

      return controller.SetAsync(installation, CancellationToken.None).OnSuccess(_ => {
        Assert.AreEqual(installation, controller.CurrentInstallation);
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
    [AsyncStateMachine(typeof(CurrentInstallationControllerTests))]
    public Task TestIsCurrent() {
      var mockInstallationIdController = new Mock<IInstallationIdController>();
      var guid = Guid.NewGuid();
      mockInstallationIdController.Setup(obj => obj.Get()).Returns(guid);

      var controller = new ParseCurrentInstallationController(mockInstallationIdController.Object);
      var installation = new ParseInstallation();
      var installation2 = new ParseInstallation();

      return controller.SetAsync(installation, CancellationToken.None).OnSuccess(t => {
        Assert.IsTrue(controller.IsCurrent(installation));
        Assert.IsFalse(controller.IsCurrent(installation2));

        controller.ClearFromMemory();

        Assert.IsFalse(controller.IsCurrent(installation));

        return controller.SetAsync(installation, CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsTrue(controller.IsCurrent(installation));
        Assert.IsFalse(controller.IsCurrent(installation2));

        controller.ClearFromDisk();

        Assert.IsFalse(controller.IsCurrent(installation));

        return controller.SetAsync(installation2, CancellationToken.None);
      }).Unwrap()
      .OnSuccess(t => {
        Assert.IsFalse(controller.IsCurrent(installation));
        Assert.IsTrue(controller.IsCurrent(installation2));
      });
    }
  }
}
