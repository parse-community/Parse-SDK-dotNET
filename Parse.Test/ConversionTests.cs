using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Utilities;

namespace Parse.Test
{
    [TestClass]
    public class ConversionTests
    {
        struct DummyValueTypeA { }

        struct DummyValueTypeB { }

        [TestMethod]
        public void TestToWithConstructedNullablePrimitive() => Assert.IsTrue(Conversion.To<int?>((double) 4) is int?);

        [TestMethod]
        public void TestToWithConstructedNullableNonPrimitive() => Assert.ThrowsException<InvalidCastException>(() => Conversion.To<DummyValueTypeA?>(new DummyValueTypeB { }));



        [TestMethod]
        public void TestConvertToFloatUsingNonInvariantNumberFormat()
        {
            try
            {
                float inputValue = 1234.56f;
                string jsonEncoded = Common.Internal.Json.Encode(inputValue);
                float convertedValue = (float) Conversion.ConvertTo<float>(jsonEncoded);
                Assert.IsTrue(inputValue == convertedValue);
            }
            catch (Exception ex)
            { throw ex; }
        }

    }
}
