using Moq;
using Parse;
using Parse.Core.Internal;
using Parse.Push.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParseTest
{
    [TestClass]
    public class InstallationTests
    {
        [TestInitialize]
        public void SetUp()
        {
            ParseObject.RegisterSubclass<ParseInstallation>();
        }

        [TestCleanup]
        public void TearDown()
        {
            ParseCorePlugins.Instance = null;
        }

        [TestMethod]
        public void TestGetInstallationQuery()
        {
            Assert.IsInstanceOfType(ParseInstallation.Query, typeof (ParseQuery<ParseInstallation>));
        }

        [TestMethod]
        public void TestInstallationIdGetterSetter()
        {
            var guid = Guid.NewGuid();
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "installationId", guid.ToString() }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual(guid, installation.InstallationId);

            var newGuid = Guid.NewGuid();
            Assert.ThrowsException<InvalidOperationException>(() => installation["installationId"] = newGuid);
            installation.SetIfDifferent("installationId", newGuid.ToString());
            Assert.AreEqual(newGuid, installation.InstallationId);
        }

        [TestMethod]
        public void TestDeviceTypeGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "deviceType", "parseOS" }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("parseOS", installation.DeviceType);

            Assert.ThrowsException<InvalidOperationException>(() => installation["deviceType"] = "gogoOS");
            installation.SetIfDifferent("deviceType", "gogoOS");
            Assert.AreEqual("gogoOS", installation.DeviceType);
        }

        [TestMethod]
        public void TestAppNameGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "appName", "parseApp" }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("parseApp", installation.AppName);

            Assert.ThrowsException<InvalidOperationException>(() => installation["appName"] = "gogoApp");
            installation.SetIfDifferent("appName", "gogoApp");
            Assert.AreEqual("gogoApp", installation.AppName);
        }

        [TestMethod]
        public void TestAppVersionGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "appVersion", "1.2.3" }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("1.2.3", installation.AppVersion);

            Assert.ThrowsException<InvalidOperationException>(() => installation["appVersion"] = "1.2.4");
            installation.SetIfDifferent("appVersion", "1.2.4");
            Assert.AreEqual("1.2.4", installation.AppVersion);
        }

        [TestMethod]
        public void TestAppIdentifierGetterSetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "appIdentifier", "com.parse.app" }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("com.parse.app", installation.AppIdentifier);

            Assert.ThrowsException<InvalidOperationException>(() => installation["appIdentifier"] = "com.parse.newapp");
            installation.SetIfDifferent("appIdentifier", "com.parse.newapp");
            Assert.AreEqual("com.parse.newapp", installation.AppIdentifier);
        }

        [TestMethod]
        public void TestTimeZoneGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "timeZone", "America/Los_Angeles" }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("America/Los_Angeles", installation.TimeZone);
        }

        [TestMethod]
        public void TestLocaleIdentifierGetter()
        {
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "localeIdentifier", "en-US" }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("en-US", installation.LocaleIdentifier);
        }

        [TestMethod]
        public void TestChannelGetterSetter()
        {
            var channels = new List<string>() { "the", "richard" };
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "channels", channels }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            Assert.IsNotNull(installation);
            Assert.AreEqual("the", installation.Channels[0]);
            Assert.AreEqual("richard", installation.Channels[1]);

            installation.Channels = new List<string>() { "mr", "kevin" };
            Assert.AreEqual("mr", installation.Channels[0]);
            Assert.AreEqual("kevin", installation.Channels[1]);
        }

        [TestMethod]
        public void TestGetCurrentInstallation()
        {
            var guid = Guid.NewGuid();
            IObjectState state = new MutableObjectState
            {
                ServerData = new Dictionary<string, object>() {
          { "installationId", guid.ToString() }
        }
            };
            ParseInstallation installation = ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
            var mockController = new Mock<IParseCurrentInstallationController>();
            mockController.Setup(obj => obj.GetAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(installation));

            ParsePushPlugins.Instance = new ParsePushPlugins
            {
                CurrentInstallationController = mockController.Object
            };

            var currentInstallation = ParseInstallation.CurrentInstallation;
            Assert.IsNotNull(currentInstallation);
            Assert.AreEqual(guid, currentInstallation.InstallationId);
        }
    }
}
