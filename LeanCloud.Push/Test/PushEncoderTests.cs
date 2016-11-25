using NUnit.Framework;
using LeanCloud.Push.Internal;
using System;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class PushEncoderTests {
    [Test]
    public void TestEncodeEmpty() {
      MutableAVState state = new MutableAVState();

      Assert.Throws<InvalidOperationException>(() => AVPushEncoder.Instance.Encode(state));
      state.Alert = "alert";

      Assert.Throws<InvalidOperationException>(() => AVPushEncoder.Instance.Encode(state));
      state.Channels = new List<string> { { "channel" } };

      Assert.DoesNotThrow(() => AVPushEncoder.Instance.Encode(state));
    }

    [Test]
    public void TestEncode() {
      MutableAVState state = new MutableAVState {
        Data = new Dictionary<string, object> {
          { "alert", "Some Alert" }
        },
        Channels = new List<string> {
          { "channel" }
        }
      };

      IDictionary<string, object> expected = new Dictionary<string, object> {
        {
          "data", new Dictionary<string, object> {{
            "alert", "Some Alert"
          }}
        },
        {
          "where", new Dictionary<string, object> {{
            "channels", new Dictionary<string, object> {{
              "$in", new List<string> {{ "channel" }}
            }}
          }}
        }
      };

      Assert.AreEqual(expected, AVPushEncoder.Instance.Encode(state));
    }
  }
}
