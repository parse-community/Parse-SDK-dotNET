// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

namespace LeanCloud {
  /// <summary>
  /// Represents a distance between two ParseGeoPoints.
  /// </summary>
  public struct AVGeoDistance {
    private const double EarthMeanRadiusKilometers = 6371.0;
    private const double EarthMeanRadiusMiles = 3958.8;

    /// <summary>
    /// Creates a AVGeoDistance.
    /// </summary>
    /// <param name="radians">The distance in radians.</param>
    public AVGeoDistance(double radians)
      : this() {
      Radians = radians;
    }

    /// <summary>
    /// Gets the distance in radians.
    /// </summary>
    public double Radians { get; private set; }

    /// <summary>
    /// Gets the distance in miles.
    /// </summary>
    public double Miles {
      get {
        return Radians * EarthMeanRadiusMiles;
      }
    }

    /// <summary>
    /// Gets the distance in kilometers.
    /// </summary>
    public double Kilometers {
      get {
        return Radians * EarthMeanRadiusKilometers;
      }
    }

    /// <summary>
    /// Gets a AVGeoDistance from a number of miles.
    /// </summary>
    /// <param name="miles">The number of miles.</param>
    /// <returns>A AVGeoDistance for the given number of miles.</returns>
    public static AVGeoDistance FromMiles(double miles) {
      return new AVGeoDistance(miles / EarthMeanRadiusMiles);
    }

    /// <summary>
    /// Gets a AVGeoDistance from a number of kilometers.
    /// </summary>
    /// <param name="kilometers">The number of kilometers.</param>
    /// <returns>A AVGeoDistance for the given number of kilometers.</returns>
    public static AVGeoDistance FromKilometers(double kilometers) {
      return new AVGeoDistance(kilometers / EarthMeanRadiusKilometers);
    }

    /// <summary>
    /// Gets a AVGeoDistance from a number of radians.
    /// </summary>
    /// <param name="radians">The number of radians.</param>
    /// <returns>A AVGeoDistance for the given number of radians.</returns>
    public static AVGeoDistance FromRadians(double radians) {
      return new AVGeoDistance(radians);
    }
  }
}
