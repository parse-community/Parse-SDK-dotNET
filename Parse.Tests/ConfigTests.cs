using System.Collections.Generic;
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
        private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true }, new MutableServiceHub { });

        private IParseConfigurationController MockedConfigController
        {
            get
            {
                var mockedConfigController = new Mock<IParseConfigurationController>();
                var mockedCurrentConfigController = new Mock<IParseCurrentConfigurationController>();

                var theConfig = Client.BuildConfiguration(new Dictionary<string, object>
                {
                    ["params"] = new Dictionary<string, object> { ["testKey"] = "testValue" }
                });

                mockedCurrentConfigController
                    .Setup(obj => obj.GetCurrentConfigAsync(Client))
                    .ReturnsAsync(theConfig);

                mockedConfigController
                    .Setup(obj => obj.CurrentConfigurationController)
                    .Returns(mockedCurrentConfigController.Object);

                mockedConfigController
                    .Setup(obj => obj.FetchConfigAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
                    .Returns(Task.FromCanceled<ParseConfiguration>(new CancellationToken(true)));

                mockedConfigController
                    .Setup(obj => obj.FetchConfigAsync(It.IsAny<string>(), It.IsAny<IServiceHub>(), It.Is<CancellationToken>(ct => !ct.IsCancellationRequested)))
                    .ReturnsAsync(theConfig);

                return mockedConfigController.Object;
            }
        }

        [TestInitialize]
        public void SetUp() =>
            (Client.Services as OrchestrationServiceHub).Custom = new MutableServiceHub
            {
                ConfigurationController = MockedConfigController,
                CurrentUserController = Mock.Of<IParseCurrentUserController>()
            };

        [TestCleanup]
        public void TearDown() => ((Client.Services as OrchestrationServiceHub).Default as ServiceHub).Reset();

        [TestMethod]
        public async void TestCurrentConfig()
        {
            var config = await Client.GetCurrentConfiguration();

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        public async void TestToJSON()
        {
            var expectedJson = new Dictionary<string, object>
            {
                ["params"] = new Dictionary<string, object> { ["testKey"] = "testValue" }
            };

            var actualJson = (await Client.GetCurrentConfiguration() as IJsonConvertible).ConvertToJSON();
            Assert.AreEqual(JsonConvert.SerializeObject(expectedJson), JsonConvert.SerializeObject(actualJson));
        }

        [TestMethod]
        public async Task TestGetConfigAsync()
        {
            var config = await Client.GetConfigurationAsync();

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        public async Task TestGetConfigCancelAsync()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await Client.GetConfigurationAsync(tokenSource.Token);
            });
        }
    }
}
