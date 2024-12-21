using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Configuration;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
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
        [Description("Tests TestCurrentConfig Returns the right config")]
        public async Task TestCurrentConfig()// Mock difficulty: 1
        {
            var config = await Client.GetCurrentConfiguration();

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        [Description("Tests the conversion of properties to json objects")]
        public async Task TestToJSON() // Mock difficulty: 1
        {
            var expectedJson = new Dictionary<string, object>
            {
                ["params"] = new Dictionary<string, object> { ["testKey"] = "testValue" }
            };

            var actualJson = (await Client.GetCurrentConfiguration() as IJsonConvertible).ConvertToJSON();
            Assert.AreEqual(JsonConvert.SerializeObject(expectedJson), JsonConvert.SerializeObject(actualJson));
        }


        [TestMethod]
        [Description("Tests the fetching of a new config with an IServiceHub instance.")]
        public async Task TestGetConfigAsync()// Mock difficulty: 1
        {
            var config = await Client.GetConfigurationAsync();

            Assert.AreEqual("testValue", config["testKey"]);
            Assert.AreEqual("testValue", config.Get<string>("testKey"));
        }

        [TestMethod]
        [Description("Tests fetching of config is cancelled when requested via a cancellation token.")]
        public async Task TestGetConfigCancelAsync() // Mock difficulty: 1
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await Client.GetConfigurationAsync(tokenSource.Token);
            });
        }
    }
    [TestClass]
    public class ParseConfigurationTests
    {
       
        [TestMethod]
        [Description("Tests that Get method throws an exception if key is not found")]
        public void Get_ThrowsExceptionNotFound() // Mock difficulty: 1
        {
            var services = new Mock<IServiceHub>().Object;
            ParseConfiguration configuration = new(services);
            Assert.ThrowsException<KeyNotFoundException>(() => configuration.Get<string>("doesNotExist"));
        }
      

        [TestMethod]
        [Description("Tests that create function creates correct configuration object")]
        public void Create_BuildsConfigurationFromDictionary() // Mock difficulty: 3
        {
            var mockDecoder = new Mock<IParseDataDecoder>();
            var mockServices = new Mock<IServiceHub>();
            var dict = new Dictionary<string, object>
            {
                ["params"] = new Dictionary<string, object> { { "test", 1 } },
            };
            mockDecoder.Setup(d => d.Decode(It.IsAny<object>(), It.IsAny<IServiceHub>())).Returns(new Dictionary<string, object> { { "test", 1 } });

            var config = ParseConfiguration.Create(dict, mockDecoder.Object, mockServices.Object);
            Assert.AreEqual(1, config["test"]);
            Assert.IsInstanceOfType(config, typeof(ParseConfiguration));
        }
    }


}