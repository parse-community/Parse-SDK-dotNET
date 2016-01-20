// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Parse.Internal.Analytics.Controller;

using Parse.Internal;

namespace Parse {
    public static partial class ParseAnalytics {
        /// <summary>
        /// Tracks this application being launched. If the LaunchActivatedEventArgs
        /// parameter contains push data passed through from a Toast's "launch"
        /// parameter, then we extract and report information to correlate this
        /// application open with that push.
        /// </summary>
        /// <param name="launchArgs">The LaunchActivatedEventArgs available in an
        /// Application.OnLaunched override.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackAppOpenedAsync(ILaunchActivatedEventArgs launchArgs) {
            // Short-circuit if the Launch event isn't from an actual app launch.
            // We'll still phone home if the launchArgs passed in is null, though,
            // so here we only check for a non-Launch ActivationKind.
            if (launchArgs != null && launchArgs.Kind != ActivationKind.Launch) {
                return ((Task)null).Safe();
            }

            IDictionary<string, object> contentJson = ParsePush.PushJson(launchArgs);
            object alert;
            string pushHash = null;
            if(contentJson.TryGetValue("alert", out alert)) {
                pushHash = ParseAnalyticsUtilities.MD5DigestFromPushPayload(alert);
            }
            return ParseAnalytics.TrackAppOpenedWithPushHashAsync(pushHash);
        }
    }
}
