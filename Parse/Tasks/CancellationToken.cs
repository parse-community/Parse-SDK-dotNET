using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading {
  /// <summary>
  /// A token that can be used for cancellation of an asynchronous operation.
  /// </summary>
  public struct CancellationToken {
    private CancellationTokenSource source;
    internal CancellationToken(CancellationTokenSource source) {
      this.source = source;
    }

    /// <summary>
    /// Gets an empty CancellationToken that cannot be cancelled.
    /// </summary>
    public static CancellationToken None {
      get {
        return default(CancellationToken);
      }
    }

    /// <summary>
    /// Gets whether cancellation has been requested for this token.
    /// </summary>
    public bool IsCancellationRequested {
      get {
        return source != null && source.IsCancellationRequested;
      }
    }

    /// <summary>
    /// Registers a callback to be invoked when this CancellationToken is cancelled.
    /// </summary>
    /// <param name="callback">The action to be invoked.</param>
    /// <returns>A registration object that can be used to deregister the callback.</returns>
    public CancellationTokenRegistration Register(Action callback) {
      if (source != null) {
        return source.Register(callback);
      }
      return default(CancellationTokenRegistration);
    }

    /// <summary>
    /// Throws an OperationCanceledException if the token has been cancelled.
    /// </summary>
    public void ThrowIfCancellationRequested() {
      if (IsCancellationRequested) {
        throw new OperationCanceledException();
      }
    }
  }
}
