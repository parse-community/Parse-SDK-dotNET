using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class ClientTests
{
    [TestMethod]
    public void TestParseClientConstructor()
    {
        ParseClient client = new("appId", "https://parse.example.com/", "dotnetKey");
        Assert.AreEqual("appId", client.ServerConnectionData.ApplicationID);
        Assert.AreEqual("https://parse.example.com/", client.ServerConnectionData.ServerURI);
        Assert.AreEqual("dotnetKey", client.ServerConnectionData.Key);
    }

    [TestMethod]
    public void TestParseClientLiveQueryConstructor()
    {
        ServerConnectionData serverConnectionData = new()
        { 
            ApplicationID = "appId", 
            ServerURI = "https://parse.example.com/",
            Key = "dotnetKey" 
        };

        LiveQueryServerConnectionData liveQueryServerConnectionData = new()
        { 
            ApplicationID = "appId", 
            ServerURI = "https://parse.example.com/", 
            Key = "dotnetKey" 
        };

        ParseClient client = new(serverConnectionData, liveQueryServerConnectionData);
        Assert.AreEqual("appId", client.ServerConnectionData.ApplicationID);
        Assert.AreEqual("https://parse.example.com/", client.ServerConnectionData.ServerURI);
        Assert.AreEqual("dotnetKey", client.ServerConnectionData.Key);

        Assert.AreEqual("appId", client.LiveQueryServerConnectionData.ApplicationID);
        Assert.AreEqual("https://parse.example.com/", client.LiveQueryServerConnectionData.ServerURI);
        Assert.AreEqual("dotnetKey", client.LiveQueryServerConnectionData.Key);
    }

    [TestMethod]
    public void TestPublicize()
    {
        ParseClient previous = ParseClient.Instance;
        ParseClient client = new("appId", "https://parse.example.com/", "dotnetKey");
        try {
            client.Publicize();
            Assert.AreSame(client, ParseClient.Instance);
        } finally {
            previous?.Publicize();
        }
    }

    [TestMethod]
    public void TestConstructorWithTestTrue()
    {
        ServerConnectionData data = new()
        { 
            ApplicationID = "appId", 
            ServerURI = "https://parse.example.com/", 
            Key = "key", 
            Test = true 
        };
        ParseClient client = new(data);
        Assert.AreEqual("https://parse.example.com/", client.ServerConnectionData.ServerURI);
    }
}
