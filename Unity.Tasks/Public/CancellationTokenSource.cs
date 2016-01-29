using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Threading {
  /// <summary>
  /// A provider for <see cref="CancellationToken"/>s. Use the CancellationTokenSource to
  /// notify consumers of its token that they should cancel any ongoing operations.
  /// </summary>
  public sealed class CancellationTokenSource {
    private object mutex = new object();
    private Action actions;

    internal CancellationTokenRegistration Register(Action action) {
      lock (mutex) {
        actions += action;
        return new CancellationTokenRegistration(this, action);
      }
    }

    internal void Unregister(Action action) {
      lock (mutex) {
        actions -= action;
      }
    }

    private bool isCancellationRequested;
    internal bool IsCancellationRequested {
      get {
        lock (mutex) {
          return isCancellationRequested;
        }
      }
    }

    /// <summary>
    /// Gets a cancellation token linked to this CancellationTokenSource.
    /// </summary>
    public CancellationToken Token {
      get {
        return new CancellationToken(this);
      }
    }

    /// <summary>
    /// Notifies consumers of the token that cancellation was requested.
    /// </summary>
    public void Cancel() {
      Cancel(false);
    }

    /// <summary>
    /// Notifies consumers of the token that cancellation was requested.
    /// If <paramref name="throwOnFirstException"/> is true, any exception thrown by a
    /// handler of the cancellation request will cause processing of the cancellation
    /// to halt and the exception will propagate immediately to the caller.
    /// </summary>
    /// <param name="throwOnFirstException">Whether to throw on first exception.</param>
    public void Cancel(bool throwOnFirstException) {
      lock (mutex) {
        isCancellationRequested = true;
        if (actions != null) {
          try {
            if (throwOnFirstException) {
              actions();
            } else {
              foreach (var del in actions.GetInvocationList()) {
                var exceptions = new List<Exception>();
                try {
                  ((Action)del)();
                } catch (Exception ex) {
                  exceptions.Add(ex);
                }
                if (exceptions.Count > 0) {
                  throw new AggregateException(exceptions);
                }
              }
            }
          } finally {
            actions = null;
          }
        }
      }
    }
  }
}
