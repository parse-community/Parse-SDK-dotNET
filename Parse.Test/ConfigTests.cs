using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Management;

namespace Parse.Test
{
    [TestClass]
    public class ConfigTests
    {
        private IParseConfigurationController MockedConfigController
        {
            get
            {
                Mock<IParseConfigurationController> mockedConfigController = new Mock<IParseConfigurationController>();
                Mock<IParseCurrentConfigurationController> mockedCurrentConfigController = new Mock<IParseCurrentConfigurationController>();

                ParseConfiguration theConfig = ParseConfigExtensions.Create(new Dictionary<string, object> { ["params"] = new Dictionary<string, object> { ["testKey"] = "testValue" } });

                mockedCurrentConfigController.Setup(obj => obj.GetCurrentConfigAsync()).Returns(Task.FromResult(theConfig));

                mockedConfigController.Setup(obj => obj.CurrentConfigurationController).Returns(mockedCurrentConfigController.Object);

                TaskCompletionSource<ParseConfiguration> tcs = new TaskCompletionSource<ParseConfiguration>();
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
            ParseConfiguration config = ParseConfiguration.CurrentConfig;

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        public void TestToJSON()
        {
            IDictionary<string, object> expectedJson = new Dictionary<string, object> { { "params", new Dictionary<string, object> { { "testKey", "testValue" } } } };
            Assert.AreEqual(JsonConvert.SerializeObject((ParseConfiguration.CurrentConfig as IJsonConvertible).ConvertToJSON()), JsonConvert.SerializeObject(expectedJson));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ConfigTests))]
        public Task TestGetConfig() => ParseConfiguration.GetAsync().ContinueWith(t =>
        {
            Assert.AreEqual("testValue", t.Result["testKey"]);
            Assert.AreEqual("testValue", t.Result.Get<string>("testKey"));
        });

        [TestMethod]
        [AsyncStateMachine(typeof(ConfigTests))]
        public Task TestGetConfigCancel()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            return ParseConfiguration.GetAsync(tokenSource.Token).ContinueWith(t => Assert.IsTrue(t.IsCanceled));
        }
    }
}
