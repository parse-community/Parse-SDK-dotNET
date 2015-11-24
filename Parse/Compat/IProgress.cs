using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System {
  /// <summary>
  /// An interface for handlers of progress update notifications.
  /// </summary>
  /// <typeparam name="T">The type of progress notifications that will be sent.</typeparam>
  public interface IProgress<in T> {
    /// <summary>
    /// Reports a change in progress to the handler.
    /// </summary>
    /// <param name="value">The new progress value.</param>
    void Report(T value);
  }
}
