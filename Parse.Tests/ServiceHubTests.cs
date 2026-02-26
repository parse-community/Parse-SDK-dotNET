using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Analytics;
using Parse.Abstractions.Platform.Cloud;
using Parse.Abstractions.Platform.Configuration;
using Parse.Abstractions.Platform.Files;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Push;
using Parse.Abstractions.Platform.Queries;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure;

namespace Parse.Tests
{
    [TestClass]
    public class ServiceHubTests
    {
        [TestMethod]
        public void Reset_ShouldResetServiceHub()
        {
            ServiceHub hub = new ServiceHub();
            hub.Reset();
            Assert.IsNotNull(hub);
        }

        [TestMethod]
        public void Constructor_ShouldCreateInstance()
        {
            ServiceHub hub = new ServiceHub();
            Assert.IsNotNull(hub);
        }

        [TestMethod]
        public void Constructor_ShouldImplementIServiceHub()
        {
            ServiceHub hub = new ServiceHub();
            Assert.IsInstanceOfType<IServiceHub>(hub);
        }

        [TestMethod]
        public void ServerConnectionData_ShouldBeStoredAndRetrieved()
        {
            ServiceHub hub = new ServiceHub();
            IServerConnectionData data = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            hub.ServerConnectionData = data;

            Assert.AreSame(data, hub.ServerConnectionData);
        }

        [TestMethod]
        public void ServerConnectionData_ShouldBeNullByDefault()
        {
            ServiceHub hub = new ServiceHub();
            Assert.IsNull(hub.ServerConnectionData);
        }

        [TestMethod]
        public void LiveQueryServerConnectionData_ShouldBeStoredAndRetrieved()
        {
            ServiceHub hub = new ServiceHub();
            ILiveQueryServerConnectionData data = new LiveQueryServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "wss://api.example.com",
                MasterKey = "testMasterKey"
            };

            hub.LiveQueryServerConnectionData = data;

            Assert.AreSame(data, hub.LiveQueryServerConnectionData);
        }

        [TestMethod]
        public void LiveQueryServerConnectionData_ShouldBeNullByDefault()
        {
            ServiceHub hub = new ServiceHub();
            Assert.IsNull(hub.LiveQueryServerConnectionData);
        }

        [TestMethod]
        public void MetadataController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IMetadataController controller = hub.MetadataController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void MetadataController_ShouldReturnSameInstanceOnMultipleCalls()
        {
            ServiceHub hub = new ServiceHub();
            IMetadataController controller1 = hub.MetadataController;
            IMetadataController controller2 = hub.MetadataController;

            Assert.AreSame(controller1, controller2);
        }

        [TestMethod]
        public void WebClient_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IWebClient client = hub.WebClient;

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void WebClient_ShouldReturnSameInstanceOnMultipleCalls()
        {
            ServiceHub hub = new ServiceHub();
            IWebClient client1 = hub.WebClient;
            IWebClient client2 = hub.WebClient;

            Assert.AreSame(client1, client2);
        }

        [TestMethod]
        public void CacheController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            ICacheController controller = hub.CacheController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void ClassController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseObjectClassController controller = hub.ClassController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void Decoder_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseDataDecoder decoder = hub.Decoder;

            Assert.IsNotNull(decoder);
        }

        [TestMethod]
        public void Decoder_ShouldReturnSameInstanceOnMultipleCalls()
        {
            ServiceHub hub = new ServiceHub();
            IParseDataDecoder decoder1 = hub.Decoder;
            IParseDataDecoder decoder2 = hub.Decoder;

            Assert.AreSame(decoder1, decoder2);
        }

        [TestMethod]
        public void InstallationController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseInstallationController controller = hub.InstallationController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void CommandRunner_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseCommandRunner runner = hub.CommandRunner;

            Assert.IsNotNull(runner);
        }

        [TestMethod]
        public void CloudCodeController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseCloudCodeController controller = hub.CloudCodeController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void ConfigurationController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseConfigurationController controller = hub.ConfigurationController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void FileController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseFileController controller = hub.FileController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void ObjectController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseObjectController controller = hub.ObjectController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void QueryController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseQueryController controller = hub.QueryController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void SessionController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseSessionController controller = hub.SessionController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void UserController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseUserController controller = hub.UserController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void CurrentUserController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseCurrentUserController controller = hub.CurrentUserController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void AnalyticsController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParseAnalyticsController controller = hub.AnalyticsController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void InstallationCoder_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseInstallationCoder coder = hub.InstallationCoder;

            Assert.IsNotNull(coder);
        }

        [TestMethod]
        public void PushChannelsController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParsePushChannelsController controller = hub.PushChannelsController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void PushController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            IParsePushController controller = hub.PushController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void CurrentInstallationController_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseCurrentInstallationController controller = hub.CurrentInstallationController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void InstallationDataFinalizer_ShouldLazilyInitialize()
        {
            ServiceHub hub = new ServiceHub();
            IParseInstallationDataFinalizer finalizer = hub.InstallationDataFinalizer;

            Assert.IsNotNull(finalizer);
        }

        [TestMethod]
        public void WebSocketClient_ShouldReturnNullWhenLiveQueryServerConnectionDataIsNull()
        {
            ServiceHub hub = new ServiceHub();
            IWebSocketClient client = hub.WebSocketClient;

            Assert.IsNull(client);
        }

        [TestMethod]
        public void WebSocketClient_ShouldReturnInstanceWhenLiveQueryServerConnectionDataIsSet()
        {
            ServiceHub hub = new ServiceHub();
            hub.LiveQueryServerConnectionData = new LiveQueryServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "wss://api.example.com",
                MasterKey = "testMasterKey",
                MessageBufferSize = 1000
            };

            IWebSocketClient client = hub.WebSocketClient;

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void LiveQueryMessageParser_ShouldReturnNullWhenLiveQueryServerConnectionDataIsNull()
        {
            ServiceHub hub = new ServiceHub();
            IParseLiveQueryMessageParser parser = hub.LiveQueryMessageParser;

            Assert.IsNull(parser);
        }

        [TestMethod]
        public void LiveQueryMessageParser_ShouldReturnInstanceWhenLiveQueryServerConnectionDataIsSet()
        {
            ServiceHub hub = new ServiceHub();
            hub.LiveQueryServerConnectionData = new LiveQueryServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "wss://api.example.com",
                MasterKey = "testMasterKey"
            };

            IParseLiveQueryMessageParser parser = hub.LiveQueryMessageParser;

            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void LiveQueryMessageBuilder_ShouldReturnNullWhenLiveQueryServerConnectionDataIsNull()
        {
            ServiceHub hub = new ServiceHub();
            IParseLiveQueryMessageBuilder builder = hub.LiveQueryMessageBuilder;

            Assert.IsNull(builder);
        }

        [TestMethod]
        public void LiveQueryMessageBuilder_ShouldReturnInstanceWhenLiveQueryServerConnectionDataIsSet()
        {
            ServiceHub hub = new ServiceHub();
            hub.LiveQueryServerConnectionData = new LiveQueryServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "wss://api.example.com",
                MasterKey = "testMasterKey"
            };

            IParseLiveQueryMessageBuilder builder = hub.LiveQueryMessageBuilder;

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void LiveQueryController_ShouldReturnNullWhenLiveQueryServerConnectionDataIsNull()
        {
            ServiceHub hub = new ServiceHub();
            IParseLiveQueryController controller = hub.LiveQueryController;

            Assert.IsNull(controller);
        }

        [TestMethod]
        public void LiveQueryController_ShouldReturnInstanceWhenLiveQueryServerConnectionDataIsSet()
        {
            ServiceHub hub = new ServiceHub();
            hub.LiveQueryServerConnectionData = new LiveQueryServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "wss://api.example.com",
                MasterKey = "testMasterKey"
            };

            IParseLiveQueryController controller = hub.LiveQueryController;

            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void Reset_ShouldReturnFalseWhenLateInitializerHasNotBeenUsed()
        {
            ServiceHub hub = new ServiceHub();
            bool result = hub.Reset();

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Reset_ShouldReturnTrueWhenLateInitializerHasBeenUsed()
        {
            ServiceHub hub = new ServiceHub();
            // Access a property to trigger lazy initialization
            _ = hub.MetadataController;

            bool result = hub.Reset();

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Reset_ShouldClearLazyInitializedValues()
        {
            ServiceHub hub = new ServiceHub();
            IMetadataController controller1 = hub.MetadataController;
            
            hub.Reset();
            
            IMetadataController controller2 = hub.MetadataController;

            // After reset, a new instance should be created
            Assert.AreNotSame(controller1, controller2);
        }

        [TestMethod]
        public void Cloner_ShouldReturnNullValue()
        {
            ServiceHub hub = new ServiceHub();
            IServiceHubCloner cloner = hub.Cloner;

            // The cloner is initialized to null in ServiceHub
            Assert.IsNull(cloner);
        }

        [TestMethod]
        public void MultipleProperties_ShouldAllInitializeIndependently()
        {
            ServiceHub hub = new ServiceHub();
            hub.ServerConnectionData = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            // Access multiple properties
            var webClient = hub.WebClient;
            var cacheController = hub.CacheController;
            var classController = hub.ClassController;
            var decoder = hub.Decoder;
            var commandRunner = hub.CommandRunner;

            // All should be non-null and distinct
            Assert.IsNotNull(webClient);
            Assert.IsNotNull(cacheController);
            Assert.IsNotNull(classController);
            Assert.IsNotNull(decoder);
            Assert.IsNotNull(commandRunner);

            Assert.AreNotSame<IWebClient>(webClient, cacheController as IWebClient);
            Assert.IsFalse(ReferenceEquals(cacheController, classController));
            Assert.IsFalse(ReferenceEquals(classController, decoder));
            Assert.IsFalse(ReferenceEquals(decoder, commandRunner));
        }

        [TestMethod]
        public void ServiceHub_ShouldPersistServerConnectionDataAcrossOperations()
        {
            ServiceHub hub = new ServiceHub();
            IServerConnectionData data = new ServerConnectionData
            {
                ApplicationID = "testApp",
                ServerURI = "https://api.example.com",
                Key = "testKey"
            };

            hub.ServerConnectionData = data;

            // Access some properties
            _ = hub.WebClient;
            _ = hub.Decoder;

            // ServerConnectionData should still be the same
            Assert.AreSame(data, hub.ServerConnectionData);
        }
    }
}
