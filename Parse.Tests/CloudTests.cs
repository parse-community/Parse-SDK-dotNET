using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Cloud;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;

namespace Parse.Tests
{
    [TestClass]
    public class CloudTests
    {
#warning Skipped post-test-evaluation cleaning method may be needed.

        // [TestCleanup]
        // public void TearDown() => ParseCorePlugins.Instance.Reset();

        [TestMethod]
        [AsyncStateMachine(typeof(CloudTests))]
        public Task TestCloudFunctions()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Mock<IParseCloudCodeController> mockController = new Mock<IParseCloudCodeController>();
            mockController.Setup(obj => obj.CallFunctionAsync<IDictionary<string, object>>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<string>(), It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object> { ["fosco"] = "ben", ["list"] = new List<object> { 1, 2, 3 } }));

            hub.CloudCodeController = mockController.Object;
            hub.CurrentUserController = new Mock<IParseCurrentUserController> { }.Object;

            return client.CallCloudCodeFunctionAsync<IDictionary<string, object>>("someFunction", null, CancellationToken.None).ContinueWith(task =>
            {
                Assert.IsFalse(task.IsFaulted);
                Assert.IsFalse(task.IsCanceled);
                Assert.IsInstanceOfType(task.Result, typeof(IDictionary<string, object>));
                Assert.AreEqual("ben", task.Result["fosco"]);
                Assert.IsInstanceOfType(task.Result["list"], typeof(IList<object>));
            });
        }
    }
}
