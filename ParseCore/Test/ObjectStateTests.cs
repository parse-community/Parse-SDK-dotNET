using NUnit.Framework;
using Parse;
using Parse.Core.Internal;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ParseTest {
  [TestFixture]
  public class ObjectStateTests {
    [Test]
    public void TestDefault() {
      IObjectState state = new MutableObjectState();
      Assert.IsNull(state.ClassName);
      Assert.IsNull(state.ObjectId);
      Assert.IsNull(state.CreatedAt);
      Assert.IsNull(state.UpdatedAt);

      Assert.DoesNotThrow(() => {
        foreach (var pair in state) {
          Assert.IsNotNull(pair);
        }
      });
    }

    [Test]
    public void TestProperties() {
      var now = new DateTime();
      IObjectState state = new MutableObjectState {
        ClassName = "Corgi",
        UpdatedAt = now,
        CreatedAt = now,
        ServerData = new Dictionary<string, object>() {
          { "1", "Choucho" },
          { "2", "Miku" },
          { "3", "Halyosy" }
        }
      };

      Assert.AreEqual("Corgi", state.ClassName);
      Assert.AreEqual(now, state.UpdatedAt);
      Assert.AreEqual(now, state.CreatedAt);
      Assert.AreEqual(3, state.Count());
      Assert.AreEqual("Choucho", state["1"]);
      Assert.AreEqual("Miku", state["2"]);
      Assert.AreEqual("Halyosy", state["3"]);
    }

    [Test]
    public void TestContainsKey() {
      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "Len", "Kagamine" },
          { "Rin", "Kagamine" },
          { "3", "Halyosy" }
        }
      };

      Assert.True(state.ContainsKey("Len"));
      Assert.True(state.ContainsKey("Rin"));
      Assert.True(state.ContainsKey("3"));
      Assert.False(state.ContainsKey("Halyosy"));
      Assert.False(state.ContainsKey("Kagamine"));
    }

    [Test]
    public void TestApplyOperation() {
      IParseFieldOperation op1 = new ParseIncrementOperation(7);
      IParseFieldOperation op2 = new ParseSetOperation("legendia");
      IParseFieldOperation op3 = new ParseSetOperation("vesperia");
      var operations = new Dictionary<string, IParseFieldOperation>() {
        { "exist", op1 },
        { "missing", op2 },
        { "change", op3 }
      };

      IObjectState state = new MutableObjectState {
        ServerData = new Dictionary<string, object>() {
          { "exist", 2 },
          { "change", "teletubies" }
        }
      };

      Assert.AreEqual(2, state["exist"]);
      Assert.AreEqual("teletubies", state["change"]);

      state = state.MutatedClone(mutableClone => {
        mutableClone.Apply(operations);
      });

      Assert.AreEqual(3, state.Count());
      Assert.AreEqual(9, state["exist"]);
      Assert.AreEqual("legendia", state["missing"]);
      Assert.AreEqual("vesperia", state["change"]);
    }

    [Test]
    public void TestApplyState() {
      var now = new DateTime();
      IObjectState state = new MutableObjectState {
        ClassName = "Corgi",
        ObjectId = "abcd",
        ServerData = new Dictionary<string, object>() {
          { "exist", 2 },
          { "change", "teletubies" }
        }
      };

      IObjectState appliedState = new MutableObjectState {
        ClassName = "AnotherCorgi",
        ObjectId = "1234",
        CreatedAt = now,
        ServerData = new Dictionary<string, object>() {
          { "exist", 9 },
          { "missing", "marasy" }
        }
      };

      state = state.MutatedClone(mutableClone => {
        mutableClone.Apply(appliedState);
      });

      Assert.AreEqual("Corgi", state.ClassName);
      Assert.AreEqual("1234", state.ObjectId);
      Assert.IsNotNull(state.CreatedAt);
      Assert.IsNull(state.UpdatedAt);
      Assert.AreEqual(3, state.Count());
      Assert.AreEqual(9, state["exist"]);
      Assert.AreEqual("teletubies", state["change"]);
      Assert.AreEqual("marasy", state["missing"]);
    }

    [Test]
    public void TestMutatedClone() {
      IObjectState state = new MutableObjectState {
        ClassName = "Corgi"
      };
      Assert.AreEqual("Corgi", state.ClassName);

      IObjectState newState = state.MutatedClone((mutableClone) => {
        mutableClone.ClassName = "AnotherCorgi";
        mutableClone.CreatedAt = new DateTime();
      });

      Assert.AreEqual("Corgi", state.ClassName);
      Assert.IsNull(state.CreatedAt);
      Assert.AreEqual("AnotherCorgi", newState.ClassName);
      Assert.IsNotNull(newState.CreatedAt);
      Assert.AreNotSame(state, newState);
    }
  }
}
