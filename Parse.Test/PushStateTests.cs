using Parse.Push.Internal;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParseTest
{
    [TestClass]
    public class PushStateTests
    {
        [TestMethod]
        public void TestMutatedClone()
        {
            MutablePushState state = new MutablePushState();

            IPushState mutated = state.MutatedClone(s =>
            {
                s.Alert = "test";
            });

            Assert.AreEqual(null, state.Alert);
            Assert.AreEqual("test", mutated.Alert);
        }

        [TestMethod]
        public void TestEquals()
        {
            MutablePushState state = new MutablePushState
            {
                Alert = "test"
            };

            MutablePushState otherState = new MutablePushState
            {
                Alert = "test"
            };

            Assert.AreNotEqual(null, state);
            Assert.AreNotEqual("test", state);

            Assert.AreEqual(state, otherState);
        }
    }
}
