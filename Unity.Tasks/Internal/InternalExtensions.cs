// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Tasks.Internal {
  /// <summary>
  /// Provides helper methods that allow us to use terser code elsewhere.
  /// </summary>
  internal static class InternalExtensions {
    internal static Task<TResult> OnSuccess<TIn, TResult>(this Task<TIn> task,
        Func<Task<TIn>, TResult> continuation) {
      return ((Task)task).OnSuccess(t => continuation((Task<TIn>)t));
    }

    internal static Task OnSuccess<TIn>(this Task<TIn> task, Action<Task<TIn>> continuation) {
      return task.OnSuccess((Func<Task<TIn>, object>)(t => {
        continuation(t);
        return null;
      }));
    }

    internal static Task<TResult> OnSuccess<TResult>(this Task task,
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

    internal static Task OnSuccess(this Task task, Action<Task> continuation) {
      return task.OnSuccess((Func<Task, object>)(t => {
        continuation(t);
        return null;
      }));
    }

    internal static Task WhileAsync(Func<Task<bool>> predicate, Func<Task> body) {
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
