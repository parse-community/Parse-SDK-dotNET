using Parse;
using Parse.Core.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class CloudControllerTests {
    [SetUp]
    public void SetUp() {
      ParseClient.Initialize(new ParseClient.Configuration {
        ApplicationId = "",
        WindowsKey = ""
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CloudControllerTests))]
    public Task TestEmptyCallFunction() {
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
      var mockRunner = CreateMockRunner(response);

      var controller = new ParseCloudCodeController(mockRunner.Object);
      return controller.CallFunctionAsync<string>("someFunction", null, null, CancellationToken.None).ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CloudControllerTests))]
    public Task TestCallFunction() {
      var responseDict = new Dictionary<string, object>() {
        { "result", "gogo" }
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new ParseCloudCodeController(mockRunner.Object);
      return controller.CallFunctionAsync<string>("someFunction", null, null, CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        Assert.AreEqual("gogo", t.Result);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CloudControllerTests))]
    public Task TestCallFunctionWithComplexType() {
      var responseDict = new Dictionary<string, object>() {
        { "result", new Dictionary<string, object>() {
          { "fosco", "ben" },
          { "list", new List<object> { 1, 2, 3 } }
        }}
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new ParseCloudCodeController(mockRunner.Object);
      return controller.CallFunctionAsync<IDictionary<string, object>>("someFunction", null, null, CancellationToken.None).ContinueWith(t => {
        Assert.IsFalse(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
        Assert.IsInstanceOf<IDictionary<string, object>>(t.Result);
        Assert.AreEqual("ben", t.Result["fosco"]);
        Assert.IsInstanceOf<IList<object>>(t.Result["list"]);
      });
    }

    [Test]
    [AsyncStateMachine(typeof(CloudControllerTests))]
    public Task TestCallFunctionWithWrongType() {
      var responseDict = new Dictionary<string, object>() {
        { "result", "gogo" }
      };
      var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
      var mockRunner = CreateMockRunner(response);

      var controller = new ParseCloudCodeController(mockRunner.Object);
      return controller.CallFunctionAsync<int>("someFunction", null, null, CancellationToken.None).ContinueWith(t => {
        Assert.True(t.IsFaulted);
        Assert.IsFalse(t.IsCanceled);
      });
    }

    private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response) {
      var mockRunner = new Mock<IParseCommandRunner>();
      mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(),
          It.IsAny<IProgress<ParseUploadProgressEventArgs>>(),
          It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(),
          It.IsAny<CancellationToken>()))
          .Returns(Task<Tuple<HttpStatusCode, IDictionary<string, object>>>.FromResult(response));

      return mockRunner;
    }
  }
}
