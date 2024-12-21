using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Queries;
using Parse.Infrastructure;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class InstallationTests
{
    private ParseClient Client { get; set; }
    private Mock<IServiceHub> ServiceHubMock { get; set; }
    private Mock<IParseObjectClassController> ClassControllerMock { get; set; }
    private Mock<IParseCurrentInstallationController> CurrentInstallationControllerMock { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize mocks
        ServiceHubMock = new Mock<IServiceHub>(MockBehavior.Strict);
        ClassControllerMock = new Mock<IParseObjectClassController>(MockBehavior.Strict);
        CurrentInstallationControllerMock = new Mock<IParseCurrentInstallationController>(MockBehavior.Strict);

        // Mock ClassController behavior
        ClassControllerMock.Setup(controller => controller.Instantiate(It.IsAny<string>(), It.IsAny<IServiceHub>()))
            .Returns<string, IServiceHub>((className, hub) => new ParseInstallation().Bind(hub) as ParseObject);

        ClassControllerMock.Setup(controller => controller.GetClassMatch("_Installation", typeof(ParseInstallation)))
            .Returns(true);

        ClassControllerMock.Setup(controller => controller.GetPropertyMappings("_Installation"))
            .Returns(new Dictionary<string, string>
            {
            { nameof(ParseInstallation.InstallationId), "installationId" },
            { nameof(ParseInstallation.DeviceType), "deviceType" },
            { nameof(ParseInstallation.AppName), "appName" },
            { nameof(ParseInstallation.AppVersion), "appVersion" },
            { nameof(ParseInstallation.AppIdentifier), "appIdentifier" },
            { nameof(ParseInstallation.TimeZone), "timeZone" },
            { nameof(ParseInstallation.LocaleIdentifier), "localeIdentifier" },
            { nameof(ParseInstallation.Channels), "channels" }
            });

        // Mock GetClassName
        ClassControllerMock.Setup(controller => controller.GetClassName(typeof(ParseInstallation)))
            .Returns("_Installation");

        ClassControllerMock.Setup(controller => controller.AddValid(It.IsAny<Type>()))
            .Verifiable();

        ServiceHubMock.Setup(hub => hub.ClassController).Returns(ClassControllerMock.Object);
        ServiceHubMock.Setup(hub => hub.CurrentInstallationController).Returns(CurrentInstallationControllerMock.Object);

        // Create ParseClient with mocked ServiceHub
        Client = new ParseClient(new ServerConnectionData { Test = true })
        {
            Services = ServiceHubMock.Object
        };

        // Publicize the client to set ParseClient.Instance
        Client.Publicize();

        // Add valid classes to the client
        Client.AddValidClass<ParseInstallation>();
    }





    [TestCleanup]
    public void TearDown()
    {
        (Client.Services as ServiceHub)?.Reset();
    }

    [TestMethod]
    public void TestInstallationPropertyMappings()
    {
        var mappings = Client.Services.ClassController.GetPropertyMappings("_Installation");
        Assert.IsNotNull(mappings);
        Assert.AreEqual("installationId", mappings[nameof(ParseInstallation.InstallationId)]);
        Assert.AreEqual("appName", mappings[nameof(ParseInstallation.AppName)]);
        Assert.AreEqual("appIdentifier", mappings[nameof(ParseInstallation.AppIdentifier)]);
        Assert.AreEqual("channels", mappings[nameof(ParseInstallation.Channels)]);
    }

    [TestMethod]
    public void TestGetInstallationQuery()
    {
        // Act: Get the query
        var query = Client.GetInstallationQuery();

        // Assert: Verify the query type and class name
        Assert.IsInstanceOfType(query, typeof(ParseQuery<ParseInstallation>));
        Assert.AreEqual("_Installation", query.ClassName);

        // Verify that GetClassName was called to resolve the class name
        ClassControllerMock.Verify(controller => controller.GetClassName(typeof(ParseInstallation)), Times.Once);

        // Verify AddValid was called for ParseInstallation
        ClassControllerMock.Verify(controller => controller.AddValid(typeof(ParseInstallation)), Times.AtLeastOnce);
    }



    [TestMethod]
    public void TestInstallationIdGetterSetter()
    {
        Guid guid = Guid.NewGuid();
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["installationId"] = guid.ToString() } },
            "_Installation");

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
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["deviceType"] = "parseOS" } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("parseOS", installation.DeviceType);

        Assert.ThrowsException<InvalidOperationException>(() => installation["deviceType"] = "gogoOS");

        installation.SetIfDifferent("deviceType", "gogoOS");
        Assert.AreEqual("gogoOS", installation.DeviceType);
    }

    [TestMethod]
    public void TestAppNameGetterSetter()
    {
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["appName"] = "parseApp" } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("parseApp", installation.AppName);

        Assert.ThrowsException<InvalidOperationException>(() => installation["appName"] = "gogoApp");

        installation.SetIfDifferent("appName", "gogoApp");
        Assert.AreEqual("gogoApp", installation.AppName);
    }

    [TestMethod]
    public void TestAppVersionGetterSetter()
    {
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["appVersion"] = "1.2.3" } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("1.2.3", installation.AppVersion);

        Assert.ThrowsException<InvalidOperationException>(() => installation["appVersion"] = "1.2.4");

        installation.SetIfDifferent("appVersion", "1.2.4");
        Assert.AreEqual("1.2.4", installation.AppVersion);
    }

    [TestMethod]
    public void TestAppIdentifierGetterSetter()
    {
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["appIdentifier"] = "com.parse.app" } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("com.parse.app", installation.AppIdentifier);

        Assert.ThrowsException<InvalidOperationException>(() => installation["appIdentifier"] = "com.parse.newapp");

        installation.SetIfDifferent("appIdentifier", "com.parse.newapp");
        Assert.AreEqual("com.parse.newapp", installation.AppIdentifier);
    }

    [TestMethod]
    public void TestTimeZoneGetter()
    {
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["timeZone"] = "America/Los_Angeles" } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("America/Los_Angeles", installation.TimeZone);
    }

    [TestMethod]
    public void TestLocaleIdentifierGetter()
    {
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["localeIdentifier"] = "en-US" } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("en-US", installation.LocaleIdentifier);
    }

    [TestMethod]
    public void TestChannelGetterSetter()
    {
        ParseInstallation installation = Client.GenerateObjectFromState<ParseInstallation>(
            new MutableObjectState { ServerData = new Dictionary<string, object> { ["channels"] = new List<string> { "the", "richard" } } },
            "_Installation");

        Assert.IsNotNull(installation);
        Assert.AreEqual("the", installation.Channels[0]);
        Assert.AreEqual("richard", installation.Channels[1]);

        installation.Channels = new List<string> { "mr", "kevin" };

        Assert.AreEqual("mr", installation.Channels[0]);
        Assert.AreEqual("kevin", installation.Channels[1]);
    }

    [TestMethod]
    public async Task TestGetCurrentInstallation()
    {
        var guid = Guid.NewGuid();
        var expectedInstallation = new ParseInstallation();
        expectedInstallation.SetIfDifferent("installationId", guid.ToString());

        CurrentInstallationControllerMock.Setup(controller => controller.GetAsync(It.IsAny<IServiceHub>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(expectedInstallation));

        ParseInstallation currentInstallation = await Client.GetCurrentInstallation();

        Assert.IsNotNull(currentInstallation);
        Assert.AreEqual(guid, currentInstallation.InstallationId);
    }
}
