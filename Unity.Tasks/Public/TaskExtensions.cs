using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Tasks.Internal;

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

    public static Task<string> ReadToEndAsync(this StreamReader reader) {
      return Task.Run(() => reader.ReadToEnd());
    }

    public static Task CopyToAsync(this Stream stream, Stream destination) {
      return stream.CopyToAsync(destination, 2048, CancellationToken.None);
    }

    public static Task CopyToAsync(this Stream stream,
        Stream destination,
        int bufferSize,
        CancellationToken cancellationToken) {
      byte[] buffer = new byte[bufferSize];
      int bytesRead = 0;
      return InternalExtensions.WhileAsync(() => {
        return stream.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask => {
          bytesRead = readTask.Result;
          return bytesRead > 0;
        });
      }, () => {
        cancellationToken.ThrowIfCancellationRequested();
        return destination.WriteAsync(buffer, 0, bytesRead, cancellationToken)
          .OnSuccess(_ => cancellationToken.ThrowIfCancellationRequested());
      });
    }

    public static Task<int> ReadAsync(this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) {
      if (cancellationToken.IsCancellationRequested) {
        var tcs = new TaskCompletionSource<int>();
        tcs.SetCanceled();
        return tcs.Task;
      }
      return Task.Factory.FromAsync<byte[], int, int, int>(stream.BeginRead,
          stream.EndRead,
            buffer,
            offset,
            count,
            null);
    }

    public static Task WriteAsync(this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) {
      if (cancellationToken.IsCancellationRequested) {
        var tcs = new TaskCompletionSource<object>();
        tcs.SetCanceled();
        return tcs.Task;
      }
      return Task.Factory.FromAsync<byte[], int, int>(stream.BeginWrite,
          stream.EndWrite,
          buffer,
          offset,
          count,
          null);
    }
  }
}
