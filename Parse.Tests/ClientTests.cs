using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;

namespace Parse.Tests;

[TestClass]
public class ClientTests
{
    [TestMethod]
    public void TestParseClientConstructor()
    {
        ParseClient client = new("appId", "https://api.dummy-parse.com/1/", "dotnetKey");
        Assert.AreEqual("appId", client.ServerConnectionData.ApplicationID);
        Assert.AreEqual("https://api.dummy-parse.com/1/", client.ServerConnectionData.ServerURI);
        Assert.AreEqual("dotnetKey", client.ServerConnectionData.Key);
    }

    [TestMethod]
    public void TestPublicize()
    {
        ParseClient previous = ParseClient.Instance;
        ParseClient client = new("appId", "https://api.dummy-parse.com/1/", "dotnetKey");
        try {
            client.Publicize();
            Assert.AreSame(client, ParseClient.Instance);
        } finally {
            previous?.Publicize();
        }
    }

    [TestMethod]
    public void TestConstructorWithInvalidUri() =>
        // Should throw if using the old parse.com URI without Test=true
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new ParseClient("appId", "https://api.parse.com/1/", "dotnetKey") { });// Actually, looking at the code, it throws if the URI is EXACTLY "https://api.parse.com/1/"// and Test is not true.

    [TestMethod]
    public void TestConstructorWithTestTrue()
    {
        ServerConnectionData data = new()
        { 
            ApplicationID = "appId", 
            ServerURI = "https://api.parse.com/1/", 
            Key = "key", 
            Test = true 
        };
        ParseClient client = new(data);
        Assert.AreEqual("https://api.parse.com/1/", client.ServerConnectionData.ServerURI);
    }
}
