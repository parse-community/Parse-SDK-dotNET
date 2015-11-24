using NUnit.Framework;
using Parse.Internal;
using System;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class PushEncoderTests {
    [Test]
    public void TestEncodeEmpty() {
      MutablePushState state = new MutablePushState();

      Assert.Throws<InvalidOperationException>(() => ParsePushEncoder.Instance.Encode(state));
      state.Alert = "alert";

      Assert.Throws<InvalidOperationException>(() => ParsePushEncoder.Instance.Encode(state));
      state.Channels = new List<string> { { "channel" } };

      Assert.DoesNotThrow(() => ParsePushEncoder.Instance.Encode(state));
    }

    [Test]
    public void TestEncode() {
      MutablePushState state = new MutablePushState {
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

      Assert.AreEqual(expected, ParsePushEncoder.Instance.Encode(state));
    }
  }
}
