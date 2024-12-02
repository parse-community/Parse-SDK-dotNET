using System;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Infrastructure.Utilities;

/// <summary>
/// A helper class for enqueuing tasks
/// </summary>
public class TaskQueue
{
    /// <summary>
    /// We only need to keep the tail of the queue. Cancelled tasks will
    /// just complete normally/immediately when their turn arrives.
    /// </summary>
    private Task? Tail { get; set; } = Task.CompletedTask; // Start with a completed task to simplify logic.

    /// <summary>
    /// Gets a task that can be awaited and is dependent on the current queue's tail.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel waiting for the task.</param>
    /// <returns>A task representing the tail of the queue.</returns>
    private Task GetTaskToAwait(CancellationToken cancellationToken)
    {
        lock (Mutex)
        {
            // Ensure the returned task is cancellable even if it's already completed.
            return Tail?.ContinueWith(
                _ => { },
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default) ?? Task.CompletedTask;
        }
    }

    /// <summary>
    /// Enqueues a task to be executed after the current tail of the queue.
    /// </summary>
    /// <typeparam name="T">The type of task.</typeparam>
    /// <param name="taskStart">A function that creates a new task dependent on the current queue state.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the waiting task.</param>
    /// <returns>The newly created task.</returns>
    public T Enqueue<T>(Func<Task, T> taskStart, CancellationToken cancellationToken = default) where T : Task
    {
        T task;

        lock (Mutex)
        {
            var oldTail = Tail ?? Task.CompletedTask;

            // Create the new task using the tail task as a dependency.
            task = taskStart(GetTaskToAwait(cancellationToken));

            // Update the tail to include the newly created task.
            Tail = Task.WhenAll(oldTail, task);
        }

        return task;
    }
    /// <summary>
    /// Synchronization object to protect shared state.
    /// </summary>
    public readonly object Mutex = new();

}
