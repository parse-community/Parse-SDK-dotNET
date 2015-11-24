using NUnit.Framework;
using Parse.Internal;

namespace ParseTest {
  [TestFixture]
  public class PushStateTests {
    [Test]
    public void TestMutatedClone() {
      MutablePushState state = new MutablePushState();

      IPushState mutated = state.MutatedClone(s => {
        s.Alert = "test";
      });

      Assert.AreEqual(null, state.Alert);
      Assert.AreEqual("test", mutated.Alert);
    }

    [Test]
    public void TestEquals() {
      MutablePushState state = new MutablePushState {
        Alert = "test"
      };

      MutablePushState otherState = new MutablePushState {
        Alert = "test"
      };

      Assert.AreNotEqual(null, state);
      Assert.AreNotEqual("test", state);

      Assert.AreEqual(state, otherState);
    }
  }
}
