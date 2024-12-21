using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Control;

namespace Parse.Tests;
[TestClass]
public class ParseOperationsTests
{
    #region ParseAddUniqueOperation Tests
    [TestMethod]
    [Description("Test MergeWithPrevious null with AddUniqueOperation should return itself.")]
    public void AddUniqueOperation_MergeWithPreviousNull_ReturnsSelf()  // Mock difficulty: 1
    {
        var operation = new ParseAddUniqueOperation(new object[] { 1, 2 });
        var result = operation.MergeWithPrevious(null);
        Assert.AreEqual(operation, result);
    }

    [TestMethod]
    [Description("Test MergeWithPrevious DeleteOperation with AddUniqueOperation returns a ParseSetOperation")]
    public void AddUniqueOperation_MergeWithPreviousDelete_ReturnsSetOperation() // Mock difficulty: 1
    {
        var operation = new ParseAddUniqueOperation(new object[] { 1, 2 });
        var result = operation.MergeWithPrevious(ParseDeleteOperation.Instance);
        Assert.IsInstanceOfType(result, typeof(ParseSetOperation));
        Assert.IsTrue(new List<object> { 1, 2 }.SequenceEqual(result.Value as List<object>));

    }
    [TestMethod]
    [Description("Test MergeWithPrevious SetOperation with AddUniqueOperation creates new ParseSetOperation with previous value")]
    public void AddUniqueOperation_MergeWithPreviousSet_ReturnsSetOperation() // Mock difficulty: 2
    {
        var operation = new ParseAddUniqueOperation(new object[] { 3, 4 });
        var setOp = new ParseSetOperation(new[] { 1, 2 });

        var result = operation.MergeWithPrevious(setOp);

        Assert.IsInstanceOfType(result, typeof(ParseSetOperation));
        Assert.IsTrue(new List<object> { 1, 2, 3, 4 }.SequenceEqual(result.Value as List<object>));
    }
    
    [TestMethod]
    [Description("Test Apply adds all the values correctly and skips existing.")]
    public void AddUniqueOperation_Apply_AddsValuesAndSkipsExisting() // Mock difficulty: 1
    {
        var operation = new ParseAddUniqueOperation(new object[] { 1, 2, 3 });
        object existingList = new List<object> { 1, 4, 5 };
        var result = operation.Apply(existingList, "testKey");
        Assert.IsTrue(new List<object> { 1, 4, 5, 2, 3 }.SequenceEqual(result as List<object>));


        var operation2 = new ParseAddUniqueOperation(new object[] { 4, 6, 7 });
        var result2 = operation2.Apply(null, "testKey");

        Assert.IsTrue(new List<object> { 4, 6, 7 }.SequenceEqual(result2 as List<object>));

    }
    
    [TestMethod]
    [Description("Tests the objects return the Data as an enumerable.")]
    public void AddUniqueOperation_Objects_ReturnsEnumerableData() // Mock difficulty: 1
    {
        var operation = new ParseAddUniqueOperation(new object[] { 1, 2 });
        Assert.AreEqual(2, operation.Objects.Count());

    }
    [TestMethod]
    [Description("Test that value returns a new list of all the objects used in the ctor.")]
    public void AddUniqueOperation_Value_ReturnsDataList() // Mock difficulty: 1
    {
        var operation = new ParseAddUniqueOperation(new object[] { 1, 2 });
        var list = operation.Value as List<object>;
        Assert.AreEqual(2, list.Count);
        Assert.IsTrue(new List<object> { 1, 2 }.SequenceEqual(list));
    }
    #endregion

    #region ParseAddOperation Tests
    [TestMethod]
    [Description("Tests if MergeWithPrevious handles null and returns this.")]
    public void AddOperation_MergeWithPreviousNull_ReturnsSelf()// Mock difficulty: 1
    {
        var operation = new ParseAddOperation(new object[] { 1, 2 });
        var result = operation.MergeWithPrevious(null);
        Assert.AreEqual(operation, result);
    }
    [TestMethod]
    [Description("Test if it replaces with a ParseSetOperation on a DeleteOperation.")]
    public void AddOperation_MergeWithPreviousDelete_ReturnsSetOperation() // Mock difficulty: 1
    {
        var operation = new ParseAddOperation(new object[] { 1, 2 });
        var result = operation.MergeWithPrevious(ParseDeleteOperation.Instance);
        Assert.IsInstanceOfType(result, typeof(ParseSetOperation));
        Assert.IsTrue(new List<object> { 1, 2 }.SequenceEqual(result.Value as List<object>));

    }
    [TestMethod]
    [Description("Tests that MergeWithPrevious with another set operator merges with previous value.")]
    public void AddOperation_MergeWithPreviousSet_ReturnsSetOperation() // Mock difficulty: 2
    {
        var operation = new ParseAddOperation(new object[] { 3, 4 });
        var setOp = new ParseSetOperation(new[] { 1, 2 });
        var result = operation.MergeWithPrevious(setOp) as ParseSetOperation;

        Assert.IsInstanceOfType(result, typeof(ParseSetOperation));
        Assert.IsTrue(new List<object> { 1, 2, 3, 4 }.SequenceEqual(result.Value as List<object>));

    }

    [TestMethod]
    [Description("Tests if Apply adds all the values to the given list")]
    public void AddOperation_Apply_AddsValuesToList()// Mock difficulty: 1
    {
        var operation = new ParseAddOperation(new object[] { 1, 2, 3 });
        object existingList = new List<object> { 1, 4, 5 };
        var result = operation.Apply(existingList, "testKey");
        Assert.IsTrue(new List<object> { 1, 4, 5, 2, 3 }.SequenceEqual(result as List<object>));

        var operation2 = new ParseAddOperation(new object[] { 1, 4, 5, 6 });
        var result2 = operation2.Apply(null, "testKey");

        Assert.IsTrue(new List<object> { 1, 4, 5, 6 }.SequenceEqual(result2 as List<object>));
    }
   

    [TestMethod]
    [Description("Tests that Objects method Returns data as an enumerable")]
    public void AddOperation_Objects_ReturnsDataAsEnumerable() // Mock difficulty: 1
    {
        var operation = new ParseAddOperation(new object[] { 1, 2 });
        Assert.AreEqual(2, operation.Objects.Count());

    }
    #endregion

    #region ParseDeleteOperation Tests
    [TestMethod]
    [Description("Tests that MergeWithPrevious returns itself if previous was deleted.")]
    public void DeleteOperation_MergeWithPrevious_ReturnsSelf() // Mock difficulty: 1
    {
        var operation = ParseDeleteOperation.Instance;
        var result = operation.MergeWithPrevious(new ParseSetOperation(1));
        Assert.AreEqual(operation, result);

        result = operation.MergeWithPrevious(ParseDeleteOperation.Instance);
        Assert.AreEqual(operation, result);
        result = operation.MergeWithPrevious(new ParseAddOperation(new List<object> { 1 }));
        Assert.AreEqual(operation, result);

        result = operation.MergeWithPrevious(new ParseAddUniqueOperation(new List<object> { 1 }));
        Assert.AreEqual(operation, result);

        result = operation.MergeWithPrevious(null);
        Assert.AreEqual(operation, result);
    }

    [TestMethod]
    [Description("Tests that DeleteOperation ConvertsToJson correctly")]
    public void DeleteOperation_ConvertToJSON_EncodeToJSONObjectCorrectly() // Mock difficulty: 1
    {
        var operation = ParseDeleteOperation.Instance;
        var json = operation.ConvertToJSON();

        Assert.IsTrue(json.ContainsKey("__op"));
        Assert.AreEqual("Delete", json["__op"]);
    }

    [TestMethod]
    [Description("Tests Apply, which always returns null.")]
    public void DeleteOperation_Apply_ReturnsDeleteToken()// Mock difficulty: 1
    {
        var operation = ParseDeleteOperation.Instance;
        var result = operation.Apply(1, "test");

        Assert.AreEqual(ParseDeleteOperation.Token, result);
    }
    [TestMethod]
    [Description("Tests the value returns a null.")]
    public void DeleteOperation_Value_ReturnsNull()// Mock difficulty: 1
    {
        var operation = ParseDeleteOperation.Instance;
        Assert.IsNull(operation.Value);
    }
    #endregion
    #region ParseIncrementOperation Tests
    
    [TestMethod]
    [Description("Tests if ParseIncrementOperation correctly increments by an int.")]
    public void IncrementOperation_MergeWithPreviousNull_ReturnsSelf()// Mock difficulty: 1
    {
        var operation = new ParseIncrementOperation(5);
        var result = operation.MergeWithPrevious(null);
        Assert.AreEqual(operation, result);

    }

    [TestMethod]
    [Description("Test if merging delete returns set.")]
    public void IncrementOperation_MergeWithPreviousDelete_ReturnsSetOperation()// Mock difficulty: 1
    {
        var operation = new ParseIncrementOperation(1);
        var result = operation.MergeWithPrevious(ParseDeleteOperation.Instance);

        Assert.IsInstanceOfType(result, typeof(ParseSetOperation));
        Assert.AreEqual(1, (int) result.Value);

    }
    [TestMethod]
    [Description("Tests If MergeWithPrevious with set merges correctly and returns type")]
    public void IncrementOperation_MergeWithPreviousSet_ReturnsCorrectType()// Mock difficulty: 2
    {
        var operation = new ParseIncrementOperation(5);
        var setOp = new ParseSetOperation(5);

        var result = operation.MergeWithPrevious(setOp);
        Assert.IsInstanceOfType(result, typeof(ParseSetOperation));
        Assert.AreEqual(10, (int) result.Value);
    }

    [TestMethod]
    [Description("Tests MergeWithPrevious throws exceptions when there are two different types")]
    public void IncrementOperation_MergeWithPreviousSetNonNumber_ThrowsException()
    {
        var operation = new ParseIncrementOperation(5);
        var setOp = new ParseSetOperation("test");

        Assert.ThrowsException<InvalidOperationException>(() => operation.MergeWithPrevious(setOp));

    }
    [TestMethod]
    [Description("Tests that MergeWithPrevious correctly increments on merge of 2 other increment operations.")]
    public void IncrementOperation_MergeWithPreviousIncrement_IncrementsValues()// Mock difficulty: 1
    {
        var operation1 = new ParseIncrementOperation(5);
        var operation2 = new ParseIncrementOperation(10);

        var result = operation1.MergeWithPrevious(operation2) as ParseIncrementOperation;

        Assert.AreEqual(15, (int) result.Amount);
    }

    [TestMethod]
    [Description("Tests that Apply correctly handles existing numbers correctly.")]
    public void IncrementOperation_Apply_IncrementsValue()// Mock difficulty: 1
    {
        var operation = new ParseIncrementOperation(5);
        object result1 = operation.Apply(10, "test");
        Assert.AreEqual(15, result1);

        object result2 = operation.Apply(10.2, "test");
        Assert.AreEqual(15.2, result2);

        object result3 = operation.Apply(null, "test");
        Assert.AreEqual(5, result3);

    }
    [TestMethod]
    [Description("Tests if Increment Operation correctly Converted To JSON.")]
    public void IncrementOperation_ConvertToJSON_EncodedToJSONObjectCorrectly() // Mock difficulty: 1
    {
        var operation = new ParseIncrementOperation(10);
        var dict = operation.ConvertToJSON();

        Assert.IsTrue(dict.ContainsKey("__op"));
        Assert.AreEqual("Increment", dict["__op"]);
        Assert.IsTrue(dict.ContainsKey("amount"));
        Assert.AreEqual(10, dict["amount"]);

    }

    [TestMethod]
    [Description("Tests the Value getter and it returns correctly")]
    public void IncrementOperation_Value_ReturnsCorrectValue() // Mock difficulty: 1
    {
        var operation = new ParseIncrementOperation(10);
        Assert.AreEqual(10, operation.Value);
    }

    [TestMethod]
    [Description("Tests apply throws on non number types")]
    public void IncrementOperation_ApplyNonNumberType_ThrowsException()// Mock difficulty: 1
    {
        var operation = new ParseIncrementOperation(10);
        Assert.ThrowsException<InvalidOperationException>(() => operation.Apply("test", "test"));

    }
    #endregion
} 