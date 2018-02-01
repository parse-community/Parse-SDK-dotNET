// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Parse.Common.Internal;
using Parse.Analytics.Internal;
using Parse.Core.Internal;

namespace Parse
{
    /// <summary>
    /// Provides an interface to Parse's logging and analytics backend.
    ///
    /// Methods will return immediately and cache requests (along with timestamps)
    /// to be handled in the background.
    /// </summary>
    public partial class ParseAnalytics
    {
        internal static IParseAnalyticsController AnalyticsController
        {
            get
            {
                return ParseAnalyticsPlugins.Instance.AnalyticsController;
            }
        }

        internal static IParseCurrentUserController CurrentUserController
        {
            get
            {
                return ParseAnalyticsPlugins.Instance.CorePlugins.CurrentUserController;
            }
        }

        /// <summary>
        /// Tracks this application being launched.
        /// </summary>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackAppOpenedAsync()
        {
            return ParseAnalytics.TrackAppOpenedWithPushHashAsync();
        }

        /// <summary>
        /// Tracks the occurrence of a custom event with additional dimensions.
        /// Parse will store a data point at the time of invocation with the
        /// given event name.
        ///
        /// Dimensions will allow segmentation of the occurrences of this
        /// custom event.
        ///
        /// To track a user signup along with additional metadata, consider the
        /// following:
        /// <code>
        /// IDictionary&lt;string, string&gt; dims = new Dictionary&lt;string, string&gt; {
        ///   { "gender", "m" },
        ///   { "source", "web" },
        ///   { "dayType", "weekend" }
        /// };
        /// ParseAnalytics.TrackEventAsync("signup", dims);
        /// </code>
        ///
        /// There is a default limit of 8 dimensions per event tracked.
        /// </summary>
        /// <param name="name">The name of the custom event to report to ParseClient
        /// as having happened.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackEventAsync(string name)
        {
            return TrackEventAsync(name, null);
        }

        /// <summary>
        /// Tracks the occurrence of a custom event with additional dimensions.
        /// Parse will store a data point at the time of invocation with the
        /// given event name.
        ///
        /// Dimensions will allow segmentation of the occurrences of this
        /// custom event.
        ///
        /// To track a user signup along with additional metadata, consider the
        /// following:
        /// <code>
        /// IDictionary&lt;string, string&gt; dims = new Dictionary&lt;string, string&gt; {
        ///   { "gender", "m" },
        ///   { "source", "web" },
        ///   { "dayType", "weekend" }
        /// };
        /// ParseAnalytics.TrackEventAsync("signup", dims);
        /// </code>
        ///
        /// There is a default limit of 8 dimensions per event tracked.
        /// </summary>
        /// <param name="name">The name of the custom event to report to ParseClient
        /// as having happened.</param>
        /// <param name="dimensions">The dictionary of information by which to
        /// segment this event.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackEventAsync(string name, IDictionary<string, string> dimensions)
        {
            if (name == null || name.Trim().Length == 0)
            {
                throw new ArgumentException("A name for the custom event must be provided.");
            }

            return CurrentUserController.GetCurrentSessionTokenAsync(CancellationToken.None)
              .OnSuccess(t =>
              {
                  return AnalyticsController.TrackEventAsync(name,
            dimensions,
            t.Result,
            CancellationToken.None);
              }).Unwrap();
        }

        /// <summary>
        /// Private method, used by platform-specific extensions to report an app-open
        /// to the server.
        /// </summary>
        /// <param name="pushHash">An identifying hash for a given push notification,
        /// passed down from the server.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        private static Task TrackAppOpenedWithPushHashAsync(string pushHash = null)
        {
            return CurrentUserController.GetCurrentSessionTokenAsync(CancellationToken.None)
              .OnSuccess(t =>
              {
                  return AnalyticsController.TrackAppOpenedAsync(pushHash,
              t.Result,
              CancellationToken.None);
              }).Unwrap();
        }
    }
}
