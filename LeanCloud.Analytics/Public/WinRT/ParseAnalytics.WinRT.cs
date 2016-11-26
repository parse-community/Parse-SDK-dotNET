// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace LeanCloud {
    public static partial class AVAnalytics {
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

            object pushHash;
            IDictionary<string, object> contentJson = PushJson(launchArgs);
            contentJson.TryGetValue("push_hash", out pushHash);
            return AVAnalytics.TrackAppOpenedWithPushHashAsync((string)pushHash);
        }

        /// <summary>
        /// Helper method to extract the full Push JSON provided to LeanCloud, including any
        /// non-visual custom information. Overloads exist for all data types which may be
        /// provided by Windows, I.E. LaunchActivatedEventArgs and PushNotificationReceivedEventArgs.
        /// Returns an empty dictionary if this push type cannot include non-visual data or
        /// this event was not triggered by a push.
        /// </summary>
        private static IDictionary<string, object> PushJson(ILaunchActivatedEventArgs eventArgs) {
          if (eventArgs == null ||
              eventArgs.Kind != ActivationKind.Launch ||
              eventArgs.Arguments == null) {
            return new Dictionary<string, object>();
          }
          return PushJson(eventArgs.Arguments);
        }

        private static IDictionary<string, object> PushJson(string jsonString) {
          try {
            return Json.Parse(jsonString) as IDictionary<string, object> ?? new Dictionary<string, object>();
          } catch (Exception) {
            return new Dictionary<string, object>();
          }
        }
    }
}
