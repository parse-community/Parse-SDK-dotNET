using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Utilities;

namespace Parse.Tests;

// TODO: Add more tests.

[TestClass]
public class LateInitializerTests
{
    LateInitializer LateInitializer { get; } = new LateInitializer { };

    [TestInitialize]
    public void Clear() => LateInitializer.Reset();

    [DataTestMethod, DataRow("Bruh", "Hello"), DataRow("Cheese", ""), DataRow("", "Waffle"), DataRow("Toaster", "Toad"), DataRow(default, "Duck"), DataRow("Dork", default)]
    public void TestAlteredValueGetValuePostGenerationCall(string initialValue, string finalValue)
    {
        string GetValue() => LateInitializer.GetValue(() => initialValue);
        bool SetValue() => LateInitializer.SetValue(finalValue);

        Assert.AreEqual(initialValue, GetValue());

        Assert.IsTrue(SetValue());
        Assert.AreNotEqual(initialValue, GetValue());
        Assert.AreEqual(finalValue, GetValue());
    }

    [DataTestMethod, DataRow("Bruh", "Hello"), DataRow("Cheese", ""), DataRow("", "Waffle"), DataRow("Toaster", "Toad"), DataRow(default, "Duck"), DataRow("Dork", default)]
    public void TestInitialGetValueCallPostSetValueCall(string initialValue, string finalValue)
    {
        string GetValue() => LateInitializer.GetValue(() => finalValue);
        bool SetValue() => LateInitializer.SetValue(initialValue);

        Assert.IsTrue(SetValue());
        Assert.AreNotEqual(finalValue, GetValue());
        Assert.AreEqual(initialValue, GetValue());
    }
}
