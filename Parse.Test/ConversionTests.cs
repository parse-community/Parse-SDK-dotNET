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
    }
}
