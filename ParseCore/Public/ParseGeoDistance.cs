// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

namespace Parse {
  /// <summary>
  /// Represents a distance between two ParseGeoPoints.
  /// </summary>
  public struct ParseGeoDistance {
    private const double EarthMeanRadiusKilometers = 6371.0;
    private const double EarthMeanRadiusMiles = 3958.8;

    /// <summary>
    /// Creates a ParseGeoDistance.
    /// </summary>
    /// <param name="radians">The distance in radians.</param>
    public ParseGeoDistance(double radians)
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
    /// Gets a ParseGeoDistance from a number of miles.
    /// </summary>
    /// <param name="miles">The number of miles.</param>
    /// <returns>A ParseGeoDistance for the given number of miles.</returns>
    public static ParseGeoDistance FromMiles(double miles) {
      return new ParseGeoDistance(miles / EarthMeanRadiusMiles);
    }

    /// <summary>
    /// Gets a ParseGeoDistance from a number of kilometers.
    /// </summary>
    /// <param name="kilometers">The number of kilometers.</param>
    /// <returns>A ParseGeoDistance for the given number of kilometers.</returns>
    public static ParseGeoDistance FromKilometers(double kilometers) {
      return new ParseGeoDistance(kilometers / EarthMeanRadiusKilometers);
    }

    /// <summary>
    /// Gets a ParseGeoDistance from a number of radians.
    /// </summary>
    /// <param name="radians">The number of radians.</param>
    /// <returns>A ParseGeoDistance for the given number of radians.</returns>
    public static ParseGeoDistance FromRadians(double radians) {
      return new ParseGeoDistance(radians);
    }
  }
}
