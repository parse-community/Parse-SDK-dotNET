using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Platform.LiveQueries;

namespace Parse.Tests;

[TestClass]
public class LiveQueryErrorEventArgsTests
{
    [TestMethod]
    public void TestParseLiveQueryErrorEventArgsConstructor()
    {
        InvalidOperationException exception = new InvalidOperationException("Test exception");
        ParseLiveQueryErrorEventArgs args = new ParseLiveQueryErrorEventArgs(42, "Test error", false, exception);

        // Assert
        Assert.AreEqual(42, args.Code);
        Assert.AreEqual("Test error", args.Error);
        Assert.AreEqual(false, args.Reconnect);
        Assert.AreEqual(exception, args.LocalException);
    }

    [TestMethod]
    public void TestParseLiveQueryErrorEventArgsConstructorWithoutException()
    {
        ParseLiveQueryErrorEventArgs args = new ParseLiveQueryErrorEventArgs(42, "Test error", true);

        // Assert
        Assert.AreEqual(42, args.Code);
        Assert.AreEqual("Test error", args.Error);
        Assert.AreEqual(true, args.Reconnect);
        Assert.AreEqual(null, args.LocalException);
    }
}
