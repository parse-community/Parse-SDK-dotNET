using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests
{
    [TestClass]
    public class InstallationTests
    {
        ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

        [TestInitialize]
        public void SetUp() => Client.AddValidClass<ParseInstallation>();

        [TestCleanup]
        public void TearDown() => (Client.Services as ServiceHub).Reset();

        [TestMethod]
        public void TestGetInstallationQuery() => Assert.IsInstanceOfType(Client.GetInstallationQuery(), typeof(ParseQuery<ParseInstallation>));

        [TestMethod]
        public void TestInstallationIdGetterSetter()
        {
            Guid guid = Guid.NewGuid();
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["installationId"] = guid.ToString() } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual(guid, installation.InstallationId);

            Guid newGuid = Guid.NewGuid();
            Assert.ThrowsException<InvalidOperationException>(() => installation["installationId"] = newGuid);

            installation.SetIfDifferent("installationId", newGuid.ToString());
            Assert.AreEqual(newGuid, installation.InstallationId);
        }

        [TestMethod]
        public void TestDeviceTypeGetterSetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["deviceType"] = "parseOS" } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("parseOS", installation.DeviceType);

            Assert.ThrowsException<InvalidOperationException>(() => installation["deviceType"] = "gogoOS");

            installation.SetIfDifferent("deviceType", "gogoOS");
            Assert.AreEqual("gogoOS", installation.DeviceType);
        }

        [TestMethod]
        public void TestAppNameGetterSetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["appName"] = "parseApp" } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("parseApp", installation.AppName);

            Assert.ThrowsException<InvalidOperationException>(() => installation["appName"] = "gogoApp");

            installation.SetIfDifferent("appName", "gogoApp");
            Assert.AreEqual("gogoApp", installation.AppName);
        }

        [TestMethod]
        public void TestAppVersionGetterSetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["appVersion"] = "1.2.3" } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("1.2.3", installation.AppVersion);

            Assert.ThrowsException<InvalidOperationException>(() => installation["appVersion"] = "1.2.4");

            installation.SetIfDifferent("appVersion", "1.2.4");
            Assert.AreEqual("1.2.4", installation.AppVersion);
        }

        [TestMethod]
        public void TestAppIdentifierGetterSetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["appIdentifier"] = "com.parse.app" } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("com.parse.app", installation.AppIdentifier);

            Assert.ThrowsException<InvalidOperationException>(() => installation["appIdentifier"] = "com.parse.newapp");

            installation.SetIfDifferent("appIdentifier", "com.parse.newapp");
            Assert.AreEqual("com.parse.newapp", installation.AppIdentifier);
        }

        [TestMethod]
        public void TestTimeZoneGetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["timeZone"] = "America/Los_Angeles" } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("America/Los_Angeles", installation.TimeZone);
        }

        [TestMethod]
        public void TestLocaleIdentifierGetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["localeIdentifier"] = "en-US" } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("en-US", installation.LocaleIdentifier);
        }

        [TestMethod]
        public void TestChannelGetterSetter()
        {
            ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["channels"] = new List<string> { "the", "richard" } } }, "_Installation");

            Assert.IsNotNull(installation);
            Assert.AreEqual("the", installation.Channels[0]);
            Assert.AreEqual("richard", installation.Channels[1]);

            installation.Channels = new List<string> { "mr", "kevin" };

            Assert.AreEqual("mr", installation.Channels[0]);
            Assert.AreEqual("kevin", installation.Channels[1]);
        }

        [TestMethod]
        public void TestGetCurrentInstallation()
        {
            MutableServiceHub hub = new MutableServiceHub { };
            ParseClient client = new ParseClient(new ServerConnectionData { Test = true }, hub);

            Guid guid = Guid.NewGuid();

            ParseInstallation installation = client.GenerateObjectFromState<ParseInstallation>(new MutableObjectState { ServerData = new Dictionary<string, object> { ["installationId"] = guid.ToString() } }, "_Installation");

            Mock<IParseCurrentInstallationController> mockController = new Mock<IParseCurrentInstallationController>();
            mockController.Setup(obj => obj.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(installation));

            hub.CurrentInstallationController = mockController.Object;

            ParseInstallation currentInstallation = client.GetCurrentInstallation();

            Assert.IsNotNull(currentInstallation);
            Assert.AreEqual(guid, currentInstallation.InstallationId);
        }
    }
}
