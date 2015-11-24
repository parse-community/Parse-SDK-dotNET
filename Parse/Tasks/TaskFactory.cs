using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading.Tasks {
  internal class TaskFactory {
    private readonly TaskScheduler scheduler;
    private readonly CancellationToken cancellationToken;

    internal TaskFactory(TaskScheduler scheduler, CancellationToken cancellationToken) {
      this.scheduler = scheduler;
      this.cancellationToken = cancellationToken;
    }

    public TaskFactory(TaskScheduler scheduler)
      : this(scheduler, CancellationToken.None) {
    }

    public TaskFactory(CancellationToken cancellationToken)
      : this(TaskScheduler.FromCurrentSynchronizationContext(), cancellationToken) {
    }

    public TaskFactory()
      : this(TaskScheduler.FromCurrentSynchronizationContext(), CancellationToken.None) {
    }

    public TaskFactory(CancellationToken cancellationToken,
        TaskCreationOptions creationOptions,
        TaskContinuationOptions continuationOptions,
        TaskScheduler scheduler)
      : this(scheduler, cancellationToken) {
      // Just ignore the other arguments -- we don't use them.
    }

    public TaskScheduler Scheduler {
      get {
        return scheduler;
      }
    }

    public Task<T> StartNew<T>(Func<T> func) {
      var tcs = new TaskCompletionSource<T>();
      scheduler.Post(() => {
        try {
          tcs.SetResult(func());
        } catch (Exception e) {
          tcs.SetException(e);
        }
      });
      return tcs.Task;
    }

    public Task FromAsync<TArg1, TArg2, TArg3>(
        Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
        Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) {
      return FromAsync((callback, _) => beginMethod(arg1, arg2, arg3, callback, state),
        endMethod, state);
    }

    public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
        Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) {
      return FromAsync((callback, _) => beginMethod(arg1, arg2, arg3, callback, state),
        endMethod, state);
    }

    public Task FromAsync<TArg1, TArg2>(
        Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
        Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) {
      return FromAsync((callback, _) => beginMethod(arg1, arg2, callback, state),
        endMethod, state);
    }

    public Task<TResult> FromAsync<TArg1, TArg2, TResult>(
        Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) {
      return FromAsync((callback, _) => beginMethod(arg1, arg2, callback, state),
        endMethod, state);
    }

    public Task FromAsync<TArg1>(
        Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
        Action<IAsyncResult> endMethod, TArg1 arg1, object state) {
      return FromAsync((callback, _) => beginMethod(arg1, callback, state),
        endMethod, state);
    }

    public Task<TResult> FromAsync<TArg1, TResult>(
        Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) {
      return FromAsync((callback, _) => beginMethod(arg1, callback, state),
        endMethod, state);
    }

    public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
        Action<IAsyncResult> endMethod,
        object state) {
      return FromAsync(beginMethod, result => {
        endMethod(result);
        return 0;
      }, state);
    }

    public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, TResult> endMethod,
        object state) {
      var tcs = new TaskCompletionSource<TResult>();
      var cancellation = cancellationToken.Register(() => tcs.TrySetCanceled());
      if (cancellationToken.IsCancellationRequested) {
        tcs.TrySetCanceled();
        cancellation.Dispose();
        return tcs.Task;
      }
      try {
        beginMethod(result => {
          try {
            var value = endMethod(result);
            tcs.TrySetResult(value);
            cancellation.Dispose();
          } catch (Exception e) {
            tcs.TrySetException(e);
            cancellation.Dispose();
          }
        }, state);
      } catch (Exception e) {
        tcs.TrySetException(e);
        cancellation.Dispose();
      }
      return tcs.Task;
    }

    public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction) {
      int remaining = tasks.Length;
      var tcs = new TaskCompletionSource<Task[]>();
      if (remaining == 0) {
        tcs.TrySetResult(tasks);
      }
      foreach (var task in tasks) {
        task.ContinueWith(_ => {
          if (Interlocked.Decrement(ref remaining) == 0) {
            tcs.TrySetResult(tasks);
          }
        });
      }
      return tcs.Task.ContinueWith(t => {
        continuationAction(t.Result);
      });
    }
  }

  internal class TaskFactory<T> {
    private readonly TaskFactory factory;

    internal TaskFactory(TaskScheduler scheduler, CancellationToken cancellationToken) {
      this.factory = new TaskFactory(scheduler, cancellationToken);
    }

    public TaskFactory(TaskScheduler scheduler)
      : this(scheduler, CancellationToken.None) {
    }

    public TaskFactory(CancellationToken cancellationToken)
      : this(TaskScheduler.FromCurrentSynchronizationContext(), cancellationToken) {
    }

    public TaskFactory()
      : this(TaskScheduler.FromCurrentSynchronizationContext(), CancellationToken.None) {
    }

    public TaskScheduler Scheduler {
      get {
        return factory.Scheduler;
      }
    }

    public Task<T> FromAsync<TArg1, TArg2, TArg3>(
        Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, T> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) {
      return factory.FromAsync<TArg1, TArg2, TArg3, T>(beginMethod, endMethod, arg1, arg2, arg3, state);
    }

    public Task<T> FromAsync<TArg1, TArg2>(
        Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, T> endMethod, TArg1 arg1, TArg2 arg2, object state) {
      return factory.FromAsync<TArg1, TArg2, T>(beginMethod, endMethod, arg1, arg2, state);
    }

    public Task<T> FromAsync<TArg1>(
        Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, T> endMethod, TArg1 arg1, object state) {
      return factory.FromAsync<TArg1, T>(beginMethod, endMethod, arg1, state);
    }

    public Task<T> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
        Func<IAsyncResult, T> endMethod,
        object state) {
      return factory.FromAsync<T>(beginMethod, endMethod, state);
    }
  }
}
