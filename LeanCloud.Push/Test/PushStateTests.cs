using NUnit.Framework;
using LeanCloud.Push.Internal;

namespace ParseTest {
  [TestFixture]
  public class PushStateTests {
    [Test]
    public void TestMutatedClone() {
      MutableAVState state = new MutableAVState();

      IAVState mutated = state.MutatedClone(s => {
        s.Alert = "test";
      });

      Assert.AreEqual(null, state.Alert);
      Assert.AreEqual("test", mutated.Alert);
    }

    [Test]
    public void TestEquals() {
      MutableAVState state = new MutableAVState {
        Alert = "test"
      };

      MutableAVState otherState = new MutableAVState {
        Alert = "test"
      };

      Assert.AreNotEqual(null, state);
      Assert.AreNotEqual("test", state);

      Assert.AreEqual(state, otherState);
    }
  }
}
