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
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    public LiveQueryEventArgsTests()
    {
        Client.Publicize();
    }

    [TestMethod]
    public void TestConstructor()
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

    [TestMethod]
    public void TestConstructorException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ParseLiveQueryEventArgs(null));
}
