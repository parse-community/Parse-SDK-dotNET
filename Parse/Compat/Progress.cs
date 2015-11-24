using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System {
  /// <summary>
  /// Provides a convenient <see cref="IProgress{T}"/> implementation for 
  /// handling progress update notifications.
  /// </summary>
  /// <typeparam name="T">The progress event argument type.</typeparam>
  public class Progress<T> : IProgress<T> where T : EventArgs {
    private SynchronizedEventHandler<T> progressChanged = new SynchronizedEventHandler<T>();

    /// <summary>
    /// Constructs a new Progress handler.
    /// </summary>
    public Progress() {
      ProgressChanged += (sender, args) => OnReport(args);
    }

    /// <summary>
    /// Constructs a new Progress handler that will invoke the given action when
    /// progress events are raised.
    /// </summary>
    /// <param name="handler">The action to invoke when progress changes.</param>
    public Progress(Action<T> handler)
      : this() {
      ProgressChanged += (sender, args) => handler(args);
      ProgressChanged += (sender, args) => OnReport(args);
    }

    void IProgress<T>.Report(T value) {
      progressChanged.Invoke(this, value);
    }

    /// <summary>
    /// A method that is called whenever progress events are raised. Override
    /// this method to handle the event.
    /// </summary>
    /// <param name="value">The updated progress.</param>
    protected virtual void OnReport(T value) {
    }

    /// <summary>
    /// An event that is raised whenever progress changes are reported.
    /// </summary>
    public event EventHandler<T> ProgressChanged {
      add {
        progressChanged.Add(value);
      }
      remove {
        progressChanged.Remove(value);
      }
    }
  }
}
