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
        [TestMethod]
        public void TestToWithConstructedNullablePrimitive() => Assert.IsTrue(Conversion.To<int?>((double) 4) is int?);
    }
}
