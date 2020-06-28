using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Configuration;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Platform.Configuration;

namespace Parse.Tests
{
    [TestClass]
    public class ConfigTests
    {
        ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true }, new MutableServiceHub { });

        IParseConfigurationController MockedConfigController
        {
            get
            {
                Mock<IParseConfigurationController> mockedConfigController = new Mock<IParseConfigurationController>();
                Mock<IParseCurrentConfigurationController> mockedCurrentConfigController = new Mock<IParseCurrentConfigurationController>();

                ParseConfiguration theConfig = Client.BuildConfiguration(new Dictionary<string, object> { ["params"] = new Dictionary<string, object> { ["testKey"] = "testValue" } });

                mockedCurrentConfigController.Setup(obj => obj.GetCurrentConfigAsync(Client)).Returns(Task.FromResult(theConfig));

                mockedConfigController.Setup(obj => obj.CurrentConfigurationController).Returns(mockedCurrentConfigController.Object);

                TaskCompletionSource<ParseConfiguration> tcs = new TaskCompletionSource<ParseConfiguration>();
                tcs.TrySetCanceled();

                mockedConfigController.Setup(obj => obj.FetchConfigAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.Is<CancellationToken>(ct => ct.IsCancellationRequested))).Returns(tcs.Task);

                mockedConfigController.Setup(obj => obj.FetchConfigAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.Is<CancellationToken>(ct => !ct.IsCancellationRequested))).Returns(Task.FromResult(theConfig));

                return mockedConfigController.Object;
            }
        }

        [TestInitialize]
        public void SetUp() => (Client.Services as OrchestrationServiceHub).Custom = new MutableServiceHub { ConfigurationController = MockedConfigController, CurrentUserController = new Mock<IParseCurrentUserController>().Object };

        [TestCleanup]
        public void TearDown() => ((Client.Services as OrchestrationServiceHub).Default as ServiceHub).Reset();

        [TestMethod]
        public void TestCurrentConfig()
        {
            ParseConfiguration config = Client.GetCurrentConfiguration();

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        public void TestToJSON()
        {
            IDictionary<string, object> expectedJson = new Dictionary<string, object> { { "params", new Dictionary<string, object> { { "testKey", "testValue" } } } };
            Assert.AreEqual(JsonConvert.SerializeObject((Client.GetCurrentConfiguration() as IJsonConvertible).ConvertToJSON()), JsonConvert.SerializeObject(expectedJson));
        }

        [TestMethod]
        [AsyncStateMachine(typeof(ConfigTests))]
        public Task TestGetConfig() => Client.GetConfigurationAsync().ContinueWith(task =>
        {
            Assert.AreEqual("testValue", task.Result["testKey"]);
            Assert.AreEqual("testValue", task.Result.Get<string>("testKey"));
        });

        [TestMethod]
        [AsyncStateMachine(typeof(ConfigTests))]
        public Task TestGetConfigCancel()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource { };
            tokenSource.Cancel();

            return Client.GetConfigurationAsync(tokenSource.Token).ContinueWith(task => Assert.IsTrue(task.IsCanceled));
        }
    }
}
