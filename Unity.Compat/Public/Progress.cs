using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System {
  /// <summary>
  /// Separate class so we don't have one synchronization context per generic instantiation of Progress.
  /// </summary>
  internal static class ProgressSynchronizationContext {
    internal static readonly SynchronizationContext SharedContext = new SynchronizationContext();
  }

  /// <summary>
  /// Provides a convenient <see cref="IProgress{T}"/> implementation for
  /// handling progress update notifications.
  /// </summary>
  /// <typeparam name="T">The progress event argument type.</typeparam>
  public class Progress<T> : IProgress<T> where T : EventArgs {
    private SynchronizationContext synchronizationContext;
    private SendOrPostCallback synchronizationCallback;
    private Action<T> eventHandler;

    /// <summary>
    /// Constructs a new Progress handler.
    /// </summary>
    public Progress() {
      synchronizationContext = SynchronizationContext.Current ?? ProgressSynchronizationContext.SharedContext;
      synchronizationCallback = NotifyDelegates;
    }

    /// <summary>
    /// Constructs a new Progress handler that will invoke the given action when
    /// progress events are raised.
    /// </summary>
    /// <param name="handler">The action to invoke when progress changes.</param>
    public Progress(Action<T> handler)
      : this() {
        eventHandler = handler;
    }

    void IProgress<T>.Report(T value) {
      OnReport(value);
    }

    /// <summary>
    /// A method that is called whenever progress events are raised. Override
    /// this method to handle the event.
    /// </summary>
    /// <param name="value">The updated progress.</param>
    protected virtual void OnReport(T value) {
      synchronizationContext.Post(synchronizationCallback, value);
    }

    /// <summary>
    /// An event that is raised whenever progress changes are reported.
    /// </summary>
    public event EventHandler<T> ProgressChanged;

    /// <summary>
    /// Notify all listening delegates of the change.
    ///
    /// Don't call this manually, only invoke it from the proper
    /// synchronization context by posting a message to it.
    /// </summary>
    private void NotifyDelegates(object newValue) {
      var value = (T)newValue;

      // Make a copy of these values so we don't run into a TOCTTOU bug.
      var handler = this.eventHandler;
      var progressChanged = ProgressChanged;

      if (handler != null) {
        handler(value);
      }

      if (progressChanged != null) {
        progressChanged(this, value);
      }
    }
  }
}
