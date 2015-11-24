using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading {
  /// <summary>
  /// Represents a registration of a handler with a cancellation token, and can be used to
  /// unregister that handler.
  /// </summary>
  public struct CancellationTokenRegistration : IDisposable {
    private Action action;
    private CancellationTokenSource source;
    internal CancellationTokenRegistration(CancellationTokenSource source, Action action) {
      this.source = source;
      this.action = action;
    }

    /// <summary>
    /// Unregisters the handler associated with this registration.
    /// </summary>
    public void Dispose() {
      if (source != null && action != null) {
        source.Unregister(action);
        action = null;
        source = null;
      }
    }
  }
}
