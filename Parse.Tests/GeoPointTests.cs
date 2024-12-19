using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parse.Infrastructure;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Tests;

[TestClass]
public class GeoPointTests
{
    ParseClient Client { get; } = new ParseClient(new ServerConnectionData { Test = true });

    [TestMethod]
    public void TestGeoPointCultureInvariantParsing()
    {
        CultureInfo initialCulture = Thread.CurrentThread.CurrentCulture;

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            Thread.CurrentThread.CurrentCulture = culture;

            ParseGeoPoint point = new ParseGeoPoint(1.234, 1.234);
            IDictionary<string, object> deserialized = Client.Decoder.Decode(JsonUtilities.Parse(JsonUtilities.Encode(new Dictionary<string, object> { [nameof(point)] = NoObjectsEncoder.Instance.Encode(point, Client) })), Client) as IDictionary<string, object>;
            ParseGeoPoint pointAgain = (ParseGeoPoint) deserialized[nameof(point)];

            Assert.AreEqual(1.234, pointAgain.Latitude);
            Assert.AreEqual(1.234, pointAgain.Longitude);
        }

        Thread.CurrentThread.CurrentCulture = initialCulture;
    }

    [TestMethod]
    public void TestGeoPointConstructor()
    {
        ParseGeoPoint point = new ParseGeoPoint();
        Assert.AreEqual(0.0, point.Latitude);
        Assert.AreEqual(0.0, point.Longitude);

        point = new ParseGeoPoint(42, 36);

        Assert.AreEqual(42.0, point.Latitude);
        Assert.AreEqual(36.0, point.Longitude);

        point.Latitude = 12;
        point.Longitude = 24;

        Assert.AreEqual(12.0, point.Latitude);
        Assert.AreEqual(24.0, point.Longitude);
    }

    [TestMethod]
    public void TestGeoPointExceptionOutOfBounds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ParseGeoPoint(90.01, 0.0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ParseGeoPoint(-90.01, 0.0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ParseGeoPoint(0.0, 180.01));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ParseGeoPoint(0.0, -180.01));
    }

    [TestMethod]
    public void TestGeoDistanceInRadians()
    {
        double d2r = Math.PI / 180.0;
        ParseGeoPoint pointA = new ParseGeoPoint();
        ParseGeoPoint pointB = new ParseGeoPoint();

        // Zero
        Assert.AreEqual(0.0, pointA.DistanceTo(pointB).Radians, 0.00001);
        Assert.AreEqual(0.0, pointB.DistanceTo(pointA).Radians, 0.00001);

        // Wrap Long
        pointA.Longitude = 179.0;
        pointB.Longitude = -179.0;
        Assert.AreEqual(2 * d2r, pointA.DistanceTo(pointB).Radians, 0.00001);
        Assert.AreEqual(2 * d2r, pointB.DistanceTo(pointA).Radians, 0.00001);

        // North South Lat
        pointA.Latitude = 89.0;
        pointA.Longitude = 0;
        pointB.Latitude = -89.0;
        pointB.Longitude = 0;
        Assert.AreEqual(178 * d2r, pointA.DistanceTo(pointB).Radians, 0.00001);
        Assert.AreEqual(178 * d2r, pointB.DistanceTo(pointA).Radians, 0.00001);

        // Long wrap Lat
        pointA.Latitude = 89.0;
        pointA.Longitude = 0;
        pointB.Latitude = -89.0;
        pointB.Longitude = 179.999;
        Assert.AreEqual(180 * d2r, pointA.DistanceTo(pointB).Radians, 0.00001);
        Assert.AreEqual(180 * d2r, pointB.DistanceTo(pointA).Radians, 0.00001);

        pointA.Latitude = 79.0;
        pointA.Longitude = 90.0;
        pointB.Latitude = -79.0;
        pointB.Longitude = -90.0;
        Assert.AreEqual(180 * d2r, pointA.DistanceTo(pointB).Radians, 0.00001);
        Assert.AreEqual(180 * d2r, pointB.DistanceTo(pointA).Radians, 0.00001);

        // Wrap near pole - somewhat ill conditioned case due to pole proximity
        pointA.Latitude = 85.0;
        pointA.Longitude = 90.0;
        pointB.Latitude = 85.0;
        pointB.Longitude = -90.0;
        Assert.AreEqual(10 * d2r, pointA.DistanceTo(pointB).Radians, 0.00001);
        Assert.AreEqual(10 * d2r, pointB.DistanceTo(pointA).Radians, 0.00001);

        // Reference cities

        // Sydney, Australia
        pointA.Latitude = -34.0;
        pointA.Longitude = 151.0;
        // Buenos Aires, Argentina
        pointB.Latitude = -34.5;
        pointB.Longitude = -58.35;
        Assert.AreEqual(1.85, pointA.DistanceTo(pointB).Radians, 0.01);
        Assert.AreEqual(1.85, pointB.DistanceTo(pointA).Radians, 0.01);
    }
}
