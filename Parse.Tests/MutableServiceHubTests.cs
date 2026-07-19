using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;

namespace Parse.Tests;

[TestClass]
public class MutableServiceHubTests
{
    [TestMethod]
    public void SetDefaults_PopulatesPropertiesAndReturnsSelf()
    {
        MutableServiceHub hub = new MutableServiceHub();
        ServerConnectionData scd = new ServerConnectionData
        {
            Test = true,
            ApplicationID = "app",
            ServerURI = "uri"
        };
        LiveQueryServerConnectionData lq = new LiveQueryServerConnectionData
        {
            Test = true,
            ApplicationID = "lapp",
            ServerURI = "luri"
        };

        MutableServiceHub result = hub.SetDefaults(scd, lq);
        Assert.AreSame(hub, result, "SetDefaults should return the same instance");

        // ensure provided data propagated
        Assert.AreEqual("app", hub.ServerConnectionData.ApplicationID);
        Assert.AreEqual("lapp", hub.LiveQueryServerConnectionData.ApplicationID);

        // verify a handful of services were defaulted
        Assert.IsNotNull(hub.WebClient);
        Assert.IsNotNull(hub.CacheController);
        Assert.IsNotNull(hub.ClassController);
        Assert.IsNotNull(hub.Decoder);
        Assert.IsNotNull(hub.UserController);

        // live-query-specific defaults should only appear when live query data is given
        Assert.IsNotNull(hub.WebSocketClient);
        Assert.IsNotNull(hub.LiveQueryController);
    }

    [TestMethod]
    public void SetDefaults_DoesNotOverrideExistingValues()
    {
        MutableServiceHub hub = new MutableServiceHub();
        UniversalWebClient custom = new UniversalWebClient();
        hub.WebClient = custom;

        hub.SetDefaults(new ServerConnectionData { Test = true });

        Assert.AreSame(custom, hub.WebClient, "Existing WebClient should not be replaced");
    }

    [TestMethod]
    public void SetDefaults_NoLiveQueryData_LeavesLiveQueryFieldsNull()
    {
        MutableServiceHub hub = new MutableServiceHub();
        hub.SetDefaults(new ServerConnectionData { Test = true }, null);

        Assert.IsNull(hub.WebSocketClient, "WebSocketClient should remain null when no live query data is provided");
        Assert.IsNull(hub.LiveQueryController, "LiveQueryController should remain null when no live query data is provided");
    }
}