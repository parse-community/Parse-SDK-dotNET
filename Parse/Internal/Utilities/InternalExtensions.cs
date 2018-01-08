// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Common.Internal {
  /// <summary>
  /// Provides helper methods that allow us to use terser code elsewhere.
  /// </summary>
  public static class InternalExtensions {
    /// <summary>
    /// Ensures a task (even null) is awaitable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
    public static Task<T> Safe<T>(this Task<T> task) {
      return task ?? Task.FromResult<T>(default(T));
    }

    /// <summary>
    /// Ensures a task (even null) is awaitable.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public static Task Safe(this Task task) {
      return task ?? Task.FromResult<object>(null);
    }

    public delegate void PartialAccessor<T>(ref T arg);

    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self,
        TKey key,
        TValue defaultValue) {
      TValue value;
      if (self.TryGetValue(key, out value)) {
        return value;
      }
      return defaultValue;
    }

    public static bool CollectionsEqual<T>(this IEnumerable<T> a, IEnumerable<T> b) {
      return Object.Equals(a, b) ||
             (a != null && b != null &&
             a.SequenceEqual(b));
    }

    public static Task<TResult> OnSuccess<TIn, TResult>(this Task<TIn> task,
        Func<Task<TIn>, TResult> continuation) {
      return ((Task)task).OnSuccess(t => continuation((Task<TIn>)t));
    }

    public static Task OnSuccess<TIn>(this Task<TIn> task, Action<Task<TIn>> continuation) {
      return task.OnSuccess((Func<Task<TIn>, object>)(t => {
        continuation(t);
        return null;
      }));
    }

    public static Task<TResult> OnSuccess<TResult>(this Task task,
        Func<Task, TResult> continuation) {
      return task.ContinueWith(t => {
        if (t.IsFaulted) {
          var ex = t.Exception.Flatten();
          if (ex.InnerExceptions.Count == 1) {
            ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
          } else {
            ExceptionDispatchInfo.Capture(ex).Throw();
          }
          // Unreachable
          return Task.FromResult(default(TResult));
        } else if (t.IsCanceled) {
          var tcs = new TaskCompletionSource<TResult>();
          tcs.SetCanceled();
          return tcs.Task;
        } else {
          return Task.FromResult(continuation(t));
        }
      }).Unwrap();
    }

    public static Task OnSuccess(this Task task, Action<Task> continuation) {
      return task.OnSuccess((Func<Task, object>)(t => {
        continuation(t);
        return null;
      }));
    }

    public static Task WhileAsync(Func<Task<bool>> predicate, Func<Task> body) {
      Func<Task> iterate = null;
      iterate = () => {
        return predicate().OnSuccess(t => {
          if (!t.Result) {
            return Task.FromResult(0);
          }
          return body().OnSuccess(_ => iterate()).Unwrap();
        }).Unwrap();
      };
      return iterate();
    }
  }
}
