// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace LeanCloud.Core.Internal {
  /// <summary>
  /// So here's the deal. We have a lot of internal APIs for AVObject, AVUser, etc.
  ///
  /// These cannot be 'internal' anymore if we are fully modularizing things out, because
  /// they are no longer a part of the same library, especially as we create things like
  /// Installation inside push library.
  ///
  /// So this class contains a bunch of extension methods that can live inside another
  /// namespace, which 'wrap' the intenral APIs that already exist.
  /// </summary>
  public static class AVObjectExtensions {
    public static T FromState<T>(IObjectState state, string defaultClassName) where T : AVObject {
      return AVObject.FromState<T>(state, defaultClassName);
    }

    public static IObjectState GetState(this AVObject obj) {
      return obj.State;
    }

    public static void HandleFetchResult(this AVObject obj, IObjectState serverState) {
      obj.HandleFetchResult(serverState);
    }

    public static IDictionary<string, IAVFieldOperation> GetCurrentOperations(this AVObject obj) {
      return obj.CurrentOperations;
    }

    public static IEnumerable<object> DeepTraversal(object root, bool traverseParseObjects = false, bool yieldRoot = false) {
      return AVObject.DeepTraversal(root, traverseParseObjects, yieldRoot);
    }

    public static void SetIfDifferent<T>(this AVObject obj, string key, T value) {
      obj.SetIfDifferent<T>(key, value);
    }

    public static IDictionary<string, object> ServerDataToJSONObjectForSerialization(this AVObject obj) {
      return obj.ServerDataToJSONObjectForSerialization();
    }

    public static void Set(this AVObject obj, string key, object value) {
      obj.Set(key, value);
    }
  }
}
