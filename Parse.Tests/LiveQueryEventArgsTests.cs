using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class LiveQueryEventArgsTests
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
    public void TestParseLiveQueryErrorEventArgsConstructor()
    {
        IObjectState state = new MutableObjectState
        {
            ObjectId = "waGiManPutr4Pet1r",
            ClassName = "Pagi",
            CreatedAt = new DateTime { },
            ServerData = new Dictionary<string, object>
            {
                ["username"] = "kevin",
                ["sessionToken"] = "se551onT0k3n"
            }
        };

        ParseObject obj = Client.GenerateObjectFromState<ParseObject>(state, "Corgi");
        ParseLiveQueryEventArgs args = new ParseLiveQueryEventArgs(obj);

        // Assert
        Assert.AreEqual(obj, args.Object);
    }
}
