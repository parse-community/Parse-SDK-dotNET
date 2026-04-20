using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Platform.LiveQueries;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class LiveQueryDualEventArgsTests
{
    private ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    public LiveQueryDualEventArgsTests()
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
        obj.Set("test", "after");
        ParseObject objOrig = Client.GenerateObjectFromState<ParseObject>(state, "Corgi");
        objOrig.Set("test", "before");
        ParseLiveQueryDualEventArgs args = new ParseLiveQueryDualEventArgs(obj, objOrig);

        Assert.AreSame(obj, args.Object);
        Assert.AreSame(objOrig, args.Original);
    }

    [TestMethod]
    public void TestCurrent()
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

        ParseObject objOrig = Client.GenerateObjectFromState<ParseObject>(state, "Corgi");
        Assert.ThrowsExactly<ArgumentNullException>(() => new ParseLiveQueryDualEventArgs(null, objOrig));
    }
   
    [TestMethod]
    public void TestConstructorExceptionOriginal()
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
        Assert.ThrowsExactly<ArgumentNullException>(() => new ParseLiveQueryDualEventArgs(obj, null));
    }
}
