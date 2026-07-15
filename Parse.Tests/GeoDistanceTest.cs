using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parse.Tests;
[TestClass]
public class ParseGeoDistanceTests
{
    [TestMethod]
    [Description("Tests that ParseGeoDistance constructor sets the value in Radians.")]
    public void Constructor_SetsRadians() 
    {
        double radians = 2.5;
        ParseGeoDistance distance = new ParseGeoDistance(radians);
        Assert.AreEqual(radians, distance.Radians);
    }
    [TestMethod]
    [Description("Tests the Miles conversion using a given Radians.")]
    public void Miles_ReturnsCorrectValue()  
    {
        double radians = 2.5;
        ParseGeoDistance distance = new ParseGeoDistance(radians);
        double expected = radians * 3958.8;
        Assert.AreEqual(expected, distance.Miles);
    }

    [TestMethod]
    [Description("Tests the Kilometers conversion using a given Radians.")]
    public void Kilometers_ReturnsCorrectValue()
    {
        double radians = 2.5;
        ParseGeoDistance distance = new ParseGeoDistance(radians);
        double expected = radians * 6371.0;

        Assert.AreEqual(expected, distance.Kilometers);
    }

    [TestMethod]
    [Description("Tests that FromMiles returns a correct ParseGeoDistance value.")]
    public void FromMiles_ReturnsCorrectGeoDistance()
    {
        double miles = 100;
        ParseGeoDistance distance = ParseGeoDistance.FromMiles(miles);
        double expected = miles / 3958.8;
        Assert.AreEqual(expected, distance.Radians);
    }

    [TestMethod]
    [Description("Tests that FromKilometers returns a correct ParseGeoDistance value.")]
    public void FromKilometers_ReturnsCorrectGeoDistance()
    {
        double kilometers = 100;
        ParseGeoDistance distance = ParseGeoDistance.FromKilometers(kilometers);
        double expected = kilometers / 6371.0;
        Assert.AreEqual(expected, distance.Radians);
    }


    [TestMethod]
    [Description("Tests that FromRadians returns a correct ParseGeoDistance value.")]
    public void FromRadians_ReturnsCorrectGeoDistance() 
    {
        double radians = 100;
        ParseGeoDistance distance = ParseGeoDistance.FromRadians(radians);
        Assert.AreEqual(radians, distance.Radians);
    }
}