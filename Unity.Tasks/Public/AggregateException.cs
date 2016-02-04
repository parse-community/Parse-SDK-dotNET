using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace System {
  /// <summary>
  /// Aggregates Exceptions that may be thrown in the process of a task's execution.
  /// </summary>
  public class AggregateException : Exception {
    /// <summary>
    /// Creates a new AggregateException from a collection of exceptions.
    /// </summary>
    /// <param name="innerExceptions">The inner exceptions.</param>
    public AggregateException(IEnumerable<Exception> innerExceptions) {
      InnerExceptions = new ReadOnlyCollection<Exception>(innerExceptions.ToList());
    }

    /// <summary>
    /// Gets the exceptions that caused this AggregateException to be raised.
    /// </summary>
    public ReadOnlyCollection<Exception> InnerExceptions {
      get;
      private set;
    }

    /// <summary>
    /// Flattens any nested AggregateExceptions into a single AggregateException
    /// whose InnerExceptions include the Exceptions of its children.
    /// </summary>
    /// <returns>A new AggregateException with no inner AggregateExceptions.</returns>
    public AggregateException Flatten() {
      var exceptions = new List<Exception>();
      foreach (var ex in InnerExceptions) {
        var ae = ex as AggregateException;
        if (ae != null) {
          exceptions.AddRange(ae.Flatten().InnerExceptions);
        } else {
          exceptions.Add(ex);
        }
      }
      return new AggregateException(exceptions);
    }

    /// <summary>
    /// Provides a summary of this Exception and all of its InnerExceptions.
    /// </summary>
    /// <returns>A string representation of this AggregateException.</returns>
    public override string ToString() {
      var sb = new StringBuilder(base.ToString());
      foreach (var inner in InnerExceptions) {
        sb.AppendLine("\n-----------------");
        sb.AppendLine(inner.ToString());
      }
      return sb.ToString();
    }
  }
}
