using Moq;
using Parse;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Test
{
    [TestClass]
    public class CloudControllerTests
    {
        [TestInitialize]
        public void SetUp() => ParseClient.Initialize(new ParseClient.Configuration { ApplicationId = "", WindowsKey = "" });

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestEmptyCallFunction()
        {
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, null);
            var mockRunner = CreateMockRunner(response);

            var controller = new ParseCloudCodeController(mockRunner.Object);
            return controller.CallFunctionAsync<string>("someFunction", null, null, CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestCallFunction()
        {
            var responseDict = new Dictionary<string, object>() {
        { "result", "gogo" }
      };
            var response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
            var mockRunner = CreateMockRunner(response);

            var controller = new ParseCloudCodeController(mockRunner.Object);
            return controller.CallFunctionAsync<string>("someFunction", null, null, CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.AreEqual("gogo", t.Result);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestCallFunctionWithComplexType()
        {
            return new ParseCloudCodeController(CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>() { { "result", new Dictionary<string, object> { { "fosco", "ben" }, { "list", new List<object> { 1, 2, 3 } } } } })).Object).CallFunctionAsync<IDictionary<string, object>>("someFunction", null, null, CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOfType(t.Result, typeof (IDictionary<string, object>));
                Assert.AreEqual("ben", t.Result["fosco"]);
                Assert.IsInstanceOfType(t.Result["list"], typeof (IList<object>));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestCallFunctionWithWrongType()
        {
            return new ParseCloudCodeController(this.CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>() { { "result", "gogo" } })).Object).CallFunctionAsync<int>("someFunction", null, null, CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsTrue(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
            });
        }

        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            var mockRunner = new Mock<IParseCommandRunner> { };
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<ParseUploadProgressEventArgs>>(), It.IsAny<IProgress<ParseDownloadProgressEventArgs>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
