using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure.Utilities;

namespace Parse.Tests
{
    [TestClass]
    public class ConversionTests
    {
        struct DummyValueTypeA { }

        struct DummyValueTypeB { }

        [TestMethod]
        public void TestToWithConstructedNullablePrimitive()
        {
            // Test conversion of double to nullable int
            var result = Conversion.To<int?>((double) 4);
            Assert.IsInstanceOfType(result, typeof(int?));
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void TestToWithConstructedNullableNonPrimitive()
        {
            // Test invalid conversion between two nullable value types
            Assert.ThrowsException<InvalidCastException>(() =>
            {
                Conversion.To<DummyValueTypeA?>(new DummyValueTypeB());
            });
        }

        [TestMethod]
        public void TestConvertToFloatUsingNonInvariantNumberFormat()
        {
            // Arrange
            float inputValue = 1234.56f;

            // Act
            string jsonEncoded = JsonUtilities.Encode(inputValue);
            float convertedValue = (float)Conversion.ConvertTo<float>(jsonEncoded);

            // Assert
            Assert.AreEqual(inputValue, convertedValue, "Converted value does not match the input value.");
        }
    }
}
