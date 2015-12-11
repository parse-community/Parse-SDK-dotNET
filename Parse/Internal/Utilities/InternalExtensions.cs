// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Internal {
  /// <summary>
  /// Provides helper methods that allow us to use terser code elsewhere.
  /// </summary>
  internal static class InternalExtensions {
    /// <summary>
    /// Ensures a task (even null) is awaitable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
    internal static Task<T> Safe<T>(this Task<T> task) {
      return task ?? Task.FromResult<T>(default(T));
    }

    /// <summary>
    /// Ensures a task (even null) is awaitable.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    internal static Task Safe(this Task task) {
      return task ?? Task.FromResult<object>(null);
    }

    internal delegate void PartialAccessor<T>(ref T arg);

    internal static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self,
        TKey key,
        TValue defaultValue) {
      TValue value;
      if (self.TryGetValue(key, out value)) {
        return value;
      }
      return defaultValue;
    }

    internal static bool CollectionsEqual<T>(this IEnumerable<T> a, IEnumerable<T> b) {
      return Object.Equals(a, b) ||
             (a != null && b != null &&
             a.SequenceEqual(b));
    }

    /// <summary>
    /// Partial methods cannot return a value, so we instead make partial accessors
    /// use ref params. This helper can be used to write code more normally so we get
    /// the out-param or default when calling a partial method. Given a partial method:
    /// partial void GetFoo(ref string foo)
    /// we can say string foo = this.GetPartial&lt;string&gt;(GetFoo);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="action"></param>
    internal static T GetPartial<T>(this ParseObject self, PartialAccessor<T> action) {
      return GetPartial(action);
    }

    internal static T GetPartial<T>(PartialAccessor<T> action) {
      T value = default(T);
      action(ref value);
      return value;
    }

    /// <summary>
    /// Partial methods cannot return a value, so we instead make partial accessors
    /// use ref params. This means you cannot effectively make a partial which is
    /// async. This code helps create a design pattern where a partial takes a ref Task
    /// param and we can await the PartialAsync of it. Given a partial method:
    /// partial void FooAsync(ref Task&lt;string&gt; task)
    /// we can say string foo = await PartialAsync&lt;string&gt;(FooAsync);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="partial"></param>
    /// <returns></returns>
    internal static Task<T> PartialAsync<T>(this object self, PartialAccessor<Task<T>> partial) {
      return PartialAsync(partial);
    }

    internal static Task<T> PartialAsync<T>(PartialAccessor<Task<T>> partial) {
      Task<T> task = null;
      partial(ref task);
      return task.Safe<T>();
    }

    internal static Task PartialAsync(this object self, PartialAccessor<Task> partial) {
      return PartialAsync(partial);
    }

    internal static Task PartialAsync(PartialAccessor<Task> partial) {
      Task task = null;
      partial(ref task);
      return task.Safe();
    }

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
