using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Parse.Infrastructure.Utilities;
/// <summary>
/// Provides helper methods that allow us to use terser code elsewhere.
/// </summary>
public static class InternalExtensions
{
    /// <summary>
    /// Ensures a task (even null) is awaitable.
    /// </summary>
    public static Task<T> Safe<T>(this Task<T> task) =>
        task ?? Task.FromResult(default(T));

    /// <summary>
    /// Ensures a task (even null) is awaitable.
    /// </summary>
    public static Task Safe(this Task task) =>
        task ?? Task.CompletedTask;

    public delegate void PartialAccessor<T>(ref T arg);

    /// <summary>
    /// Gets the value from a dictionary or returns the default value if the key is not found.
    /// </summary>
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue defaultValue) =>
        self.TryGetValue(key, out var value) ? value : defaultValue;

    /// <summary>
    /// Compares two collections for equality.
    /// </summary>
    public static bool CollectionsEqual<T>(this IEnumerable<T> a, IEnumerable<T> b) =>
        ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b));

    /// <summary>
    /// Executes a continuation on a task that returns a result on success.
    /// </summary>
    public static async Task<TResult> OnSuccess<TResult>(this Task task, Func<Task, Task<TResult>> continuation)
    {
        if (task.IsFaulted)
        {
            var ex = task.Exception?.Flatten();
            ExceptionDispatchInfo.Capture(ex?.InnerExceptions[0] ?? ex).Throw();
        }
        else if (task.IsCanceled)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetCanceled();
            return await tcs.Task.ConfigureAwait(false);
        }

        // Ensure continuation returns a Task<TResult>, then await with ConfigureAwait
        var resultTask = continuation(task);
        return await resultTask.ConfigureAwait(false);
    }


    /// <summary>
    /// Executes a continuation on a task that has a result type.
    /// </summary>
    public static async Task<TResult> OnSuccess<TIn, TResult>(this Task<TIn> task, Func<Task<TIn>, Task<TResult>> continuation)
    {
        if (task.IsFaulted)
        {
            var ex = task.Exception?.Flatten();
            ExceptionDispatchInfo.Capture(ex?.InnerExceptions[0] ?? ex).Throw();
        }
        else if (task.IsCanceled)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetCanceled();
            return await tcs.Task.ConfigureAwait(false);
        }

        // Ensure continuation returns a Task<TResult>, then await with ConfigureAwait
        var resultTask = continuation(task);
        return await resultTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a continuation on a task and returns void.
    /// </summary>
    public static async Task OnSuccess(this Task task, Action<Task> continuation)
    {
        if (task.IsFaulted)
        {
            var ex = task.Exception?.Flatten();
            ExceptionDispatchInfo.Capture(ex?.InnerExceptions[0] ?? ex).Throw();
        }
        else if (task.IsCanceled)
        {
            task = Task.CompletedTask;
        }

        continuation(task);
        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a continuation on a task and returns void, for tasks with result.
    /// </summary>
    public static async Task OnSuccess<TIn>(this Task<TIn> task, Action<Task<TIn>> continuation)
    {
        if (task.IsFaulted)
        {
            var ex = task.Exception?.Flatten();
            ExceptionDispatchInfo.Capture(ex?.InnerExceptions[0] ?? ex).Throw();
        }
        else if (task.IsCanceled)
        {
            task = Task.FromResult<TIn>(default); // Handle canceled task by returning a completed Task<TIn>
        }

        continuation(task);
        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous loop until the predicate evaluates to false.
    /// </summary>
    public static async Task WhileAsync(Func<Task<bool>> predicate, Func<Task> body)
    {
        while (await predicate().ConfigureAwait(false))
        {
            await body().ConfigureAwait(false);
        }
    }
}
