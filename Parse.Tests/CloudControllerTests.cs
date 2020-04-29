using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.Cloud;

namespace Parse.Tests
{
#warning Class refactoring requires completion.

    [TestClass]
    public class CloudControllerTests
    {
        ParseClient Client { get; set; }

        [TestInitialize]
        public void SetUp() => Client = new ParseClient(new ServerConnectionData { ApplicationID = "", Key = "", Test = true });

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestEmptyCallFunction() => new ParseCloudCodeController(CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, default)).Object, Client.Decoder).CallFunctionAsync<string>("someFunction", default, default, Client, CancellationToken.None).ContinueWith(task =>
        {
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
        });

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestCallFunction()
        {
            Dictionary<string, object> responseDict = new Dictionary<string, object> { ["result"] = "gogo" };
            Tuple<HttpStatusCode, IDictionary<string, object>> response = new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, responseDict);
            Mock<IParseCommandRunner> mockRunner = CreateMockRunner(response);

            return new ParseCloudCodeController(mockRunner.Object, Client.Decoder).CallFunctionAsync<string>("someFunction", default, default, Client, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.AreEqual("gogo", task.Result);
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestCallFunctionWithComplexType() => new ParseCloudCodeController(CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object> { { "result", new Dictionary<string, object> { { "fosco", "ben" }, { "list", new List<object> { 1, 2, 3 } } } } })).Object, Client.Decoder).CallFunctionAsync<IDictionary<string, object>>("someFunction", default, default, Client, CancellationToken.None).ContinueWith(task =>
        {
            Assert.IsFalse(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
            Assert.IsInstanceOfType(task.Result, typeof(IDictionary<string, object>));
            Assert.AreEqual("ben", task.Result["fosco"]);
            Assert.IsInstanceOfType(task.Result["list"], typeof(IList<object>));
        });

        [TestMethod]
        [AsyncStateMachine(typeof(CloudControllerTests))]
        public Task TestCallFunctionWithWrongType() => new ParseCloudCodeController(CreateMockRunner(new Tuple<HttpStatusCode, IDictionary<string, object>>(HttpStatusCode.Accepted, new Dictionary<string, object>() { { "result", "gogo" } })).Object, Client.Decoder).CallFunctionAsync<int>("someFunction", default, default, Client, CancellationToken.None).ContinueWith(task =>
        {
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
        });

        private Mock<IParseCommandRunner> CreateMockRunner(Tuple<HttpStatusCode, IDictionary<string, object>> response)
        {
            Mock<IParseCommandRunner> mockRunner = new Mock<IParseCommandRunner> { };
            mockRunner.Setup(obj => obj.RunCommandAsync(It.IsAny<ParseCommand>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<IProgress<IDataTransferLevel>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            return mockRunner;
        }
    }
}
