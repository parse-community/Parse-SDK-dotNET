using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Platform.LiveQueries;

namespace Parse.Tests;

[TestClass]
public class MessageReceivedEventArgsTests
{
    private ParseClient Client { get; set; }

    [TestInitialize]
    public void SetUp()
    {
        // Initialize the client and ensure the instance is set
        Client = new ParseClient(new ServerConnectionData { Test = true });
        Client.Publicize();
    }

    [TestCleanup]
    public void TearDown() => (Client.Services as ServiceHub).Reset();

    [TestMethod]
    public void TestParseMessageReceivedEventArgsConstructor()
    {
        string msg = "Corgi";
        MessageReceivedEventArgs args = new MessageReceivedEventArgs(msg);

        // Assert
        Assert.AreEqual(msg, args.Message);
    }

    [TestMethod]
    public void TestParseMessageReceivedEventArgsConstructorException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new MessageReceivedEventArgs(null));
}
