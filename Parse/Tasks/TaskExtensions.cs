using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading.Tasks {
  /// <summary>
  /// Provides extension methods for working with <see cref="Task"/>s.
  /// </summary>
  public static class TaskExtensions {
    /// <summary>
    /// Unwraps a nested task, producing a task that is complete when both the outer
    /// and inner tasks are complete. This is primarily useful for chaining asynchronous
    /// operations together.
    /// </summary>
    /// <param name="task">The task to unwrap.</param>
    /// <returns>A new task that is complete when both the outer and inner tasks
    /// are complete.</returns>
    public static Task Unwrap(this Task<Task> task) {
      var tcs = new TaskCompletionSource<int>();
      task.ContinueWith(t => {
        if (t.IsFaulted) {
          tcs.TrySetException(t.Exception);
        } else if (t.IsCanceled) {
          tcs.TrySetCanceled();
        } else {
          task.Result.ContinueWith(inner => {
            if (inner.IsFaulted) {
              tcs.TrySetException(inner.Exception);
            } else if (inner.IsCanceled) {
              tcs.TrySetCanceled();
            } else {
              tcs.TrySetResult(0);
            }
          });
        }
      });
      return tcs.Task;
    }

    /// <summary>
    /// Unwraps a nested task, producing a task that is complete when both the outer
    /// and inner tasks are complete and that has the inner task's result.
    /// This is primarily useful for chaining asynchronous operations together.
    /// </summary>
    /// <param name="task">The task to unwrap.</param>
    /// <returns>A new task that is complete when both the outer and inner tasks
    /// are complete and that has the inner task's result.</returns>
    public static Task<T> Unwrap<T>(this Task<Task<T>> task) {
      var tcs = new TaskCompletionSource<T>();
      task.ContinueWith(t => {
        if (t.IsFaulted) {
          tcs.TrySetException(t.Exception);
        } else if (t.IsCanceled) {
          tcs.TrySetCanceled();
        } else {
          t.Result.ContinueWith(inner => {
            if (inner.IsFaulted) {
              tcs.TrySetException(inner.Exception);
            } else if (inner.IsCanceled) {
              tcs.TrySetCanceled();
            } else {
              tcs.TrySetResult(inner.Result);
            }
          });
        }
      });
      return tcs.Task;
    }
  }
}
