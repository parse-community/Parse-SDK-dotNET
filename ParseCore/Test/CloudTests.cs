using Parse;
using Parse.Core.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ParseTest {
  [TestFixture]
  public class CloudTests {
    [TearDown]
    public void TearDown() {
      ParseCorePlugins.Instance.Reset();
    }

    [Test]
    [AsyncStateMachine(typeof(CloudTests))]
    public Task TestCloudFunctions() {
      IDictionary<string, object> response = new Dictionary<string, object>() {
        { "fosco", "ben" },
        { "list", new List<object> { 1, 2, 3 } }
      };
      var mockController = new Mock<IParseCloudCodeController>();
      mockController.Setup(obj => obj.CallFunctionAsync<IDictionary<string, object>>(It.IsAny<string>(),
          It.IsAny<IDictionary<string, object>>(),
          It.IsAny<string>(),
          It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));
      var mockCurrentUserController = new Mock<IParseCurrentUserController>();

      ParseCorePlugins plugins = new ParseCorePlugins();
      plugins.CloudCodeController = mockController.Object;
      plugins.CurrentUserController = mockCurrentUserController.Object;
      ParseCorePlugins.Instance = plugins;

      return ParseCloud.CallFunctionAsync<IDictionary<string, object>>("someFunction", null, CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        Assert.IsInstanceOf<IDictionary<string, object>>(t.Result);
        Assert.AreEqual("ben", t.Result["fosco"]);
        Assert.IsInstanceOf<IList<object>>(t.Result["list"]);
      });
    }
  }
}
