using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading.Tasks {
  /// <summary>
  /// Allows safe orchestration of a task's completion, preventing the consumer from prematurely
  /// completing the task. Essentially, it represents the producer side of a <see cref="Tasks.Task{T}"/>,
  /// providing access to the consumer side through the <see cref="TaskCompletionSource{T}.Task"/>
  /// while isolating the Task's completion mechanisms from the consumer.
  /// </summary>
  /// <typeparam name="T">The type of the result of the Task being created.</typeparam>
  public class TaskCompletionSource<T> {
    /// <summary>
    /// Constructs a new TaskCompletionSource.
    /// </summary>
    public TaskCompletionSource() {
      Task = new Task<T>();
    }

    /// <summary>
    /// Gets the task associated with this TaskCompletionSource.
    /// </summary>
    public Task<T> Task { get; private set; }

    /// <summary>
    /// If the task is not already complete, completes the task by setting the result.
    /// </summary>
    /// <param name="result">The result for the task.</param>
    /// <returns><c>true</c> if the result was set successfully.</returns>
    public bool TrySetResult(T result) {
      return Task.TrySetResult(result);
    }

    /// <summary>
    /// If the task is not already complete, completes the task by setting the exception.
    /// </summary>
    /// <param name="exception">The exception for the task.</param>
    /// <returns><c>true</c> if the exception was set successfully.</returns>
    public bool TrySetException(AggregateException exception) {
      return Task.TrySetException(exception);
    }

    /// <summary>
    /// If the task is not already complete, completes the task by setting the exception.
    /// </summary>
    /// <param name="exception">The exception for the task.</param>
    /// <returns><c>true</c> if the exception was set successfully.</returns>
    public bool TrySetException(Exception exception) {
      var aggregate = exception as AggregateException;
      if (aggregate != null) {
        return Task.TrySetException(aggregate);
      }
      return Task.TrySetException(new AggregateException(new[] { exception }).Flatten());
    }

    /// <summary>
    /// If the task is not already complete, cancels the task.
    /// </summary>
    /// <returns><c>true</c> if the task was successfully cancelled.</returns>
    public bool TrySetCanceled() {
      return Task.TrySetCanceled();
    }

    /// <summary>
    /// Completes the task by setting the result. Throws an <see cref="InvalidOperationException"/>
    /// if the task is already complete.
    /// </summary>
    /// <param name="result">The result for the task.</param>
    public void SetResult(T result) {
      if (!TrySetResult(result)) {
        throw new InvalidOperationException("Cannot set the result of a completed task.");
      }
    }

    /// <summary>
    /// Completes the task by setting the exception. Throws an
    /// <see cref="InvalidOperationException"/> if the task is already complete.
    /// </summary>
    /// <param name="exception">The exception for the task.</param>
    public void SetException(AggregateException exception) {
      if (!TrySetException(exception)) {
        throw new InvalidOperationException("Cannot set the exception of a completed task.");
      }
    }

    /// <summary>
    /// Completes the task by setting the exception. Throws an
    /// <see cref="InvalidOperationException"/> if the task is already complete.
    /// </summary>
    /// <param name="exception">The exception for the task.</param>
    public void SetException(Exception exception) {
      if (!TrySetException(exception)) {
        throw new InvalidOperationException("Cannot set the exception of a completed task.");
      }
    }

    /// <summary>
    /// Cancels the task. Throws an <see cref="InvalidOperationException"/> if the task is
    /// already complete.
    /// </summary>
    public void SetCanceled() {
      if (!TrySetCanceled()) {
        throw new InvalidOperationException("Cannot cancel a completed task.");
      }
    }
  }
}
