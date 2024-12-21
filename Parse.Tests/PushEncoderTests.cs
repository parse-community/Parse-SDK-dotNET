using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Parse.Platform.Push;

namespace Parse.Tests;

[TestClass]
public class PushEncoderTests
{
    [TestMethod]
    public void TestEncodeEmpty()
    {
        MutablePushState state = new MutablePushState();

        Assert.ThrowsException<InvalidOperationException>(() => ParsePushEncoder.Instance.Encode(state));
        state.Alert = "alert";

        Assert.ThrowsException<InvalidOperationException>(() => ParsePushEncoder.Instance.Encode(state));
        state.Channels = new List<string> { "channel" };

        ParsePushEncoder.Instance.Encode(state);
    }

    [TestMethod]
    public void TestEncode()
    {
        MutablePushState state = new MutablePushState
        {
            Data = new Dictionary<string, object>
            {
                ["alert"] = "Some Alert"
            },
            Channels = new List<string> { "channel" }
        };

        IDictionary<string, object> expected = new Dictionary<string, object>
        {
            ["data"] = new Dictionary<string, object>
            {
                ["alert"] = "Some Alert"
            },
            ["where"] = new Dictionary<string, object>
            {
                ["channels"] = new Dictionary<string, object>
                {
                    ["$in"] = new List<string> { "channel" }
                }
            }
        };

        Assert.AreEqual(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(ParsePushEncoder.Instance.Encode(state)));
    }
}
