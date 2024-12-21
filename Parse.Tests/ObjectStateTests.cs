using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Control;
using Parse.Platform.Objects;

namespace Parse.Tests;

[TestClass]
public class ObjectStateTests
{
    [TestMethod]
    public void TestDefault()
    {
        IObjectState state = new MutableObjectState();
        Assert.IsNull(state.ClassName);
        Assert.IsNull(state.ObjectId);
        Assert.IsNull(state.CreatedAt);
        Assert.IsNull(state.UpdatedAt);

        foreach (KeyValuePair<string, object> pair in state)
        {
            Assert.IsNotNull(pair);
        }
    }

    [TestMethod]
    public void TestProperties()
    {
        DateTime now = new DateTime();
        IObjectState state = new MutableObjectState
        {
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

    [TestMethod]
    public void TestContainsKey()
    {
        IObjectState state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>() {
      { "Len", "Kagamine" },
      { "Rin", "Kagamine" },
      { "3", "Halyosy" }
    }
        };

        Assert.IsTrue(state.ContainsKey("Len"));
        Assert.IsTrue(state.ContainsKey("Rin"));
        Assert.IsTrue(state.ContainsKey("3"));
        Assert.IsFalse(state.ContainsKey("Halyosy"));
        Assert.IsFalse(state.ContainsKey("Kagamine"));
    }

    [TestMethod]
    public void TestApplyOperation()
    {
        IParseFieldOperation op1 = new ParseIncrementOperation(7);
        IParseFieldOperation op2 = new ParseSetOperation("legendia");
        IParseFieldOperation op3 = new ParseSetOperation("vesperia");
        Dictionary<string, IParseFieldOperation> operations = new Dictionary<string, IParseFieldOperation>() {
    { "exist", op1 },
    { "missing", op2 },
    { "change", op3 }
  };

        IObjectState state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>() {
      { "exist", 2 },
      { "change", "teletubies" }
    }
        };

        Assert.AreEqual(2, state["exist"]);
        Assert.AreEqual("teletubies", state["change"]);

        state = state.MutatedClone(mutableClone => mutableClone.Apply(operations));

        Assert.AreEqual(3, state.Count());
        Assert.AreEqual(9, state["exist"]);
        Assert.AreEqual("legendia", state["missing"]);
        Assert.AreEqual("vesperia", state["change"]);
    }

    [TestMethod]
    public void TestApplyState()
    {
        DateTime now = new DateTime();
        IObjectState state = new MutableObjectState
        {
            ClassName = "Corgi",
            ObjectId = "abcd",
            ServerData = new Dictionary<string, object>() {
      { "exist", 2 },
      { "change", "teletubies" }
    }
        };

        IObjectState appliedState = new MutableObjectState
        {
            ClassName = "AnotherCorgi",
            ObjectId = "1234",
            CreatedAt = now,
            ServerData = new Dictionary<string, object>() {
      { "exist", 9 },
      { "missing", "marasy" }
    }
        };

        state = state.MutatedClone(mutableClone => mutableClone.Apply(appliedState));

        Assert.AreEqual("Corgi", state.ClassName);
        Assert.AreEqual("1234", state.ObjectId);
        Assert.IsNotNull(state.CreatedAt);
        Assert.IsNull(state.UpdatedAt);
        Assert.AreEqual(3, state.Count());
        Assert.AreEqual(9, state["exist"]);
        Assert.AreEqual("teletubies", state["change"]);
        Assert.AreEqual("marasy", state["missing"]);
    }

    [TestMethod]
    public void TestMutatedClone()
    {
        IObjectState state = new MutableObjectState
        {
            ClassName = "Corgi"
        };
        Assert.AreEqual("Corgi", state.ClassName);

        IObjectState newState = state.MutatedClone((mutableClone) =>
        {
            mutableClone.ClassName = "AnotherCorgi";
            mutableClone.CreatedAt = new DateTime();
        });

        Assert.AreEqual("Corgi", state.ClassName);
        Assert.IsNull(state.CreatedAt);
        Assert.AreEqual("AnotherCorgi", newState.ClassName);
        Assert.IsNotNull(newState.CreatedAt);
        Assert.AreNotSame(state, newState);
    }



    [TestMethod]
    [Description("Tests that MutableClone clones null values correctly.")]
    public void MutatedClone_WithNullValues() // Mock difficulty: 1
    {
        IObjectState state = new MutableObjectState
        {
            ObjectId = "testId"
        };

        IObjectState newState = state.MutatedClone(m =>
        {
            m.ObjectId = null;

        });

        Assert.IsNull(newState.ObjectId);
    }


    [TestMethod]
    [Description("Tests that MutatedClone ignores exceptions")]
    public void MutatedClone_IgnoresExceptions() // Mock difficulty: 1
    {
        IObjectState state = new MutableObjectState
        {
            ClassName = "Test"
        };

        IObjectState newState = state.MutatedClone(m =>
        {
            m.ClassName = "NewName";
            throw new Exception();
        });

        Assert.AreEqual("NewName", newState.ClassName);
    }
    [TestMethod]
    [Description("Tests that Decode correctly parses a Dictionary")]
    public void Decode_ParsesDictionary() // Mock difficulty: 2
    {
        var dict = new Dictionary<string, object>
            {
                { "className", "TestClass" },
                { "objectId", "testId" },
                { "createdAt", DateTime.Now },
                { "updatedAt", DateTime.Now },
                 { "isNew", true },
                { "test", 1}
            };
        IServiceHub mockHub = new Mock<IServiceHub>().Object;
        var state = MutableObjectState.Decode(dict, mockHub);

        Assert.IsNotNull(state);
        Assert.AreEqual("TestClass", state.ClassName);
        Assert.AreEqual("testId", state.ObjectId);
        Assert.IsNotNull(state.CreatedAt);
        Assert.IsNotNull(state.UpdatedAt);
        Assert.IsTrue(state.IsNew);
        Assert.AreEqual(1, state["test"]);
    }
    [TestMethod]
    [Description("Tests that decode can gracefully handle invalid values.")]
    public void Decode_HandlesInvalidValues() // Mock difficulty: 2
    {
        var dict = new Dictionary<string, object>
            {
                { "className", "TestClass" },
                { "objectId", "testId" },
                { "createdAt", "invalid date" },
                { "updatedAt", 123 },
            };
        IServiceHub mockHub = new Mock<IServiceHub>().Object;

        var state = MutableObjectState.Decode(dict, mockHub);

        Assert.IsNotNull(state);
        Assert.IsNull(state.CreatedAt);
        Assert.IsNull(state.UpdatedAt);
        Assert.AreEqual("TestClass", state.ClassName);
        Assert.AreEqual("testId", state.ObjectId);
    }
    [TestMethod]
    [Description("Tests that Decode Returns null if the data is not a Dictionary.")]
    public void Decode_ReturnsNullForInvalidData() // Mock difficulty: 1
    {
        IServiceHub mockHub = new Mock<IServiceHub>().Object;
        var state = MutableObjectState.Decode("invalidData", mockHub);
        Assert.IsNull(state);
    }
   
    [TestMethod]
    [Description("Tests Apply method ignores exceptions on invalid keys")]
    public void Apply_WithIncompatibleKey_SkipsKey() // Mock difficulty: 1
    {
        var mockOp = new Mock<IParseFieldOperation>();
        mockOp.Setup(op => op.Apply(It.IsAny<object>(), It.IsAny<string>())).Throws(new InvalidCastException());
        var operations = new Dictionary<string, IParseFieldOperation>
            {
                { "InvalidKey", mockOp.Object }
            };

        IObjectState state = new MutableObjectState
        {
            ServerData = new Dictionary<string, object>() {
                     { "testKey", 1 }
                }
        };

        state = state.MutatedClone(m => m.Apply(operations));

        Assert.AreEqual(1, state["testKey"]);
    }
 
    [TestMethod]
    [Description("Tests that when apply other state copies objectId, createdAt, updatedAt")]
    public void Apply_OtherStateCopiesCorrectly() // Mock difficulty: 1
    {
        DateTime now = DateTime.Now;
        IObjectState state = new MutableObjectState
        {
            ClassName = "test"
        };

        IObjectState appliedState = new MutableObjectState
        {
            ObjectId = "testId",
            CreatedAt = now,
            UpdatedAt = now,
            IsNew = true,
        };

        state = state.MutatedClone(mutableClone => mutableClone.Apply(appliedState));

        Assert.AreEqual("testId", state.ObjectId);
        Assert.AreEqual(now, state.CreatedAt);
        Assert.AreEqual(now, state.UpdatedAt);
        Assert.IsTrue(state.IsNew);
    }

}