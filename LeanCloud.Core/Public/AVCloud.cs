// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;

namespace LeanCloud {
  /// <summary>
  /// The AVCloud class provides methods for interacting with LeanCloud Cloud Functions.
  /// </summary>
  /// <example>
  /// For example, this sample code calls the
  /// "validateGame" Cloud Function and calls processResponse if the call succeeded
  /// and handleError if it failed.
  ///
  /// <code>
  /// var result =
  ///     await AVCloud.CallFunctionAsync&lt;IDictionary&lt;string, object&gt;&gt;("validateGame", parameters);
  /// </code>
  /// </example>
  public static class AVCloud {
    internal static IAVCloudCodeController CloudCodeController {
      get {
        return AVPlugins.Instance.CloudCodeController;
      }
    }

    /// <summary>
    /// Calls a cloud function.
    /// </summary>
    /// <typeparam name="T">The type of data you will receive from the cloud function. This
    /// can be an IDictionary, string, IList, AVObject, or any other type supported by
    /// AVObject.</typeparam>
    /// <param name="name">The cloud function to call.</param>
    /// <param name="parameters">The parameters to send to the cloud function. This
    /// dictionary can contain anything that could be passed into a AVObject except for
    /// ParseObjects themselves.</param>
    /// <returns>The result of the cloud call.</returns>
    public static Task<T> CallFunctionAsync<T>(String name, IDictionary<string, object> parameters) {
      return CallFunctionAsync<T>(name, parameters, CancellationToken.None);
    }

    /// <summary>
    /// Calls a cloud function.
    /// </summary>
    /// <typeparam name="T">The type of data you will receive from the cloud function. This
    /// can be an IDictionary, string, IList, AVObject, or any other type supported by
    /// AVObject.</typeparam>
    /// <param name="name">The cloud function to call.</param>
    /// <param name="parameters">The parameters to send to the cloud function. This
    /// dictionary can contain anything that could be passed into a AVObject except for
    /// ParseObjects themselves.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the cloud call.</returns>
    public static Task<T> CallFunctionAsync<T>(String name,
        IDictionary<string, object> parameters, CancellationToken cancellationToken) {
      return CloudCodeController.CallFunctionAsync<T>(name,
          parameters,
          AVUser.CurrentSessionToken,
          cancellationToken);
    }
  }
}
