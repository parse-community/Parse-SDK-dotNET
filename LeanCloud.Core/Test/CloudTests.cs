using LeanCloud;
using LeanCloud.Core.Internal;
using NUnit.Framework;
using Moq;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace ParseTest
{
    [TestFixture]
    public class CloudTests
    {

        [SetUp]
        public void SetUp()
        {
            string appId = ConfigurationManager.AppSettings["appId"];
            string appKey = ConfigurationManager.AppSettings["appKey"];
            AVClient.Initialize(appId, appKey);

            AVUser.LogOut();
        }

        [TearDown]
        public void TearDown()
        {
            AVPlugins.Instance.Reset();
        }

        [Test]
        [AsyncStateMachine(typeof(CloudTests))]
        public Task TestCloudFunctions()
        {
            IDictionary<string, object> response = new Dictionary<string, object>() {
                { "fosco", "ben" },
                { "list", new List<object> { 1, 2, 3 } }
            };

            var mockController = new Mock<IAVCloudCodeController>();
            mockController.Setup(obj => obj.CallFunctionAsync<IDictionary<string, object>>(It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));
            var mockCurrentUserController = new Mock<IAVCurrentUserController>();

            AVPlugins plugins = new AVPlugins();
            plugins.CloudCodeController = mockController.Object;
            plugins.CurrentUserController = mockCurrentUserController.Object;
            AVPlugins.Instance = plugins;

            return AVCloud.CallFunctionAsync<IDictionary<string, object>>("someFunction", null, CancellationToken.None).ContinueWith(t =>
            {
                Assert.IsFalse(t.IsFaulted);
                Assert.IsFalse(t.IsCanceled);
                Assert.IsInstanceOf<IDictionary<string, object>>(t.Result);
                Assert.AreEqual("ben", t.Result["fosco"]);
                Assert.IsInstanceOf<IList<object>>(t.Result["list"]);
            });
        }

        [Test]
        [AsyncStateMachine(typeof(CloudTests))]
        public Task TestRPCFunctionsListObject()
        {
            return AVCloud.RPCFunctionAsync<IList<AVObject>>("getTodos", null, CancellationToken.None).ContinueWith(t =>
             {
                 Assert.False(t.IsFaulted);
                 Assert.False(t.IsCanceled);
                 var w = t.Result;
                 var f = w[0];
                 Assert.True(f.ObjectId != null);
                 return Task.FromResult(0);
             });
        }

        [Test]
        [AsyncStateMachine(typeof(CloudTests))]
        public Task TestRPCFunctionsSingleObject()
        {
            return AVCloud.RPCFunctionAsync<AVObject>("getTodo", null, CancellationToken.None).ContinueWith(t =>
            {
                Assert.False(t.IsFaulted);
                Assert.False(t.IsCanceled);
                var w = t.Result;
                Assert.True(w.ObjectId != null);
                return Task.FromResult(0);
            });
        }
    }
}
