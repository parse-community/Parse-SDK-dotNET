// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Common.Internal {
  /// <summary>
  /// A helper class for enqueuing tasks
  /// </summary>
  public class TaskQueue {
    /// <summary>
    /// We only need to keep the tail of the queue. Cancelled tasks will
    /// just complete normally/immediately when their turn arrives.
    /// </summary>
    private Task tail;
    private readonly object mutex = new object();

    /// <summary>
    /// Gets a cancellable task that can be safely awaited and is dependent
    /// on the current tail of the queue. This essentially gives us a proxy
    /// for the tail end of the queue whose awaiting can be cancelled.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that cancels
    /// the task even if the task is still in the queue. This allows the
    /// running task to return immediately without breaking the dependency
    /// chain. It also ensures that errors do not propagate.</param>
    /// <returns>A new task that should be awaited by enqueued tasks.</returns>
    private Task GetTaskToAwait(CancellationToken cancellationToken) {
      lock (mutex) {
        Task toAwait = tail ?? Task.FromResult(true);
        return toAwait.ContinueWith(task => { }, cancellationToken);
      }
    }

    /// <summary>
    /// Enqueues a task created by <paramref name="taskStart"/>. If the task is
    /// cancellable (or should be able to be cancelled while it is waiting in the
    /// queue), pass a cancellationToken.
    /// </summary>
    /// <typeparam name="T">The type of task.</typeparam>
    /// <param name="taskStart">A function given a task to await once state is
    /// snapshotted (e.g. after capturing session tokens at the time of the save call).
    /// Awaiting this task will wait for the created task's turn in the queue.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to
    /// cancel waiting in the queue.</param>
    /// <returns>The task created by the taskStart function.</returns>
    public T Enqueue<T>(Func<Task, T> taskStart, CancellationToken cancellationToken)
        where T : Task {
      Task oldTail;
      T task;
      lock (mutex) {
        oldTail = this.tail ?? Task.FromResult(true);
        // The task created by taskStart is responsible for waiting the
        // task passed to it before doing its work (this gives it an opportunity
        // to do startup work or save state before waiting for its turn in the queue
        task = taskStart(GetTaskToAwait(cancellationToken));

        // The tail task should be dependent on the old tail as well as the newly-created
        // task. This prevents cancellation of the new task from causing the queue to run
        // out of order.
        this.tail = Task.WhenAll(oldTail, task);
      }
      return task;
    }

    public object Mutex { get { return mutex; } }
  }
}
