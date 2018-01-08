using Moq;
using Parse;
using Parse.Common.Internal;
using Parse.Core.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Parse.Test
{
    [TestClass]
    public class ConfigTests
    {
        private IParseConfigController MockedConfigController
        {
            get
            {
                var mockedConfigController = new Mock<IParseConfigController>();
                var mockedCurrentConfigController = new Mock<IParseCurrentConfigController>();

                ParseConfig theConfig = ParseConfigExtensions.Create(new Dictionary<string, object> { { "params", new Dictionary<string, object> { { "testKey", "testValue" } } }});

                mockedCurrentConfigController.Setup(obj => obj.GetCurrentConfigAsync()).Returns(Task.FromResult(theConfig));

                mockedConfigController.Setup(obj => obj.CurrentConfigController).Returns(mockedCurrentConfigController.Object);

                var tcs = new TaskCompletionSource<ParseConfig>();
                tcs.TrySetCanceled();

                mockedConfigController.Setup(obj => obj.FetchConfigAsync(It.IsAny<string>(), It.Is<CancellationToken>(ct => ct.IsCancellationRequested))).Returns(tcs.Task);

                mockedConfigController.Setup(obj => obj.FetchConfigAsync(It.IsAny<string>(), It.Is<CancellationToken>(ct => !ct.IsCancellationRequested))).Returns(Task.FromResult(theConfig));

                return mockedConfigController.Object;
            }
        }

        [TestInitialize]
        public void SetUp() => ParseCorePlugins.Instance = new ParseCorePlugins { ConfigController = MockedConfigController, CurrentUserController = new Mock<IParseCurrentUserController>().Object };

        [TestCleanup]
        public void TearDown() => ParseCorePlugins.Instance = null;

        [TestMethod]
        public void TestCurrentConfig()
        {
            ParseConfig config = ParseConfig.CurrentConfig;

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        public void TestToJSON()
        {
            IDictionary<string, object> expectedJson = new Dictionary<string, object> { { "params", new Dictionary<string, object> { { "testKey", "testValue" } } } };
            Assert.AreEqual(JsonConvert.SerializeObject((ParseConfig.CurrentConfig as IJsonConvertible).ToJSON()), JsonConvert.SerializeObject(expectedJson));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ConfigTests))]
        public Task TestGetConfig()
        {
            return ParseConfig.GetAsync().ContinueWith(t =>
            {
                Assert.AreEqual("testValue", t.Result["testKey"]);
                Assert.AreEqual("testValue", t.Result.Get<string>("testKey"));
            });
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ConfigTests))]
        public Task TestGetConfigCancel()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            return ParseConfig.GetAsync(tokenSource.Token).ContinueWith(t => Assert.IsTrue(t.IsCanceled));
        }
    }
}
